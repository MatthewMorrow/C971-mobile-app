using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Web;
using C971.Data;
using C971.Models;
using Plugin.LocalNotification;

namespace C971.Views
{
    public partial class CourseDetailPage : ContentPage
    {
        private readonly AppDatabase _db;
        private Course? _course;
        private readonly int? _courseId;
        private readonly int? _termId;
        public ObservableCollection<Assessment> Assessments { get; } = [];

        public CourseDetailPage(int? cid, int? tid, AppDatabase database)
        {
            InitializeComponent();
            _db = database;
            _courseId = cid;
            _termId = tid;
            AssessmentsCollection.ItemsSource = Assessments;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _ = LoadCourseAsync();
        }

        private async Task LoadCourseAsync()
        {
            if (_courseId is > 0)
            {
                var c = await _db.GetCourseAsync(_courseId.Value);
                if (c != null)
                {
                    _course = c;
                    if (!string.IsNullOrWhiteSpace(_course.Title)) CourseTitle.Text = _course.Title;
                    if (_course.StartDate != default)
                    {
                        StartDate.Date = _course.StartDate.Date;
                        StartTime.Time = _course.StartDate.TimeOfDay;
                    }

                    if (_course.EndDate != default)
                    {
                        EndDate.Date = _course.EndDate.Date;
                        EndTime.Time = _course.EndDate.TimeOfDay;
                    }

                    StatusPicker.SelectedItem = _course.Status;
                    if (!string.IsNullOrWhiteSpace(_course.InstructorName))
                        InstructorName.Text = _course.InstructorName;
                    if (!string.IsNullOrWhiteSpace(_course.InstructorPhone))
                        InstructorPhone.Text = _course.InstructorPhone;
                    if (!string.IsNullOrWhiteSpace(_course.InstructorEmail))
                        InstructorEmail.Text = _course.InstructorEmail;
                    if (!string.IsNullOrWhiteSpace(_course.Notes)) NotesEditor.Text = HttpUtility.HtmlDecode(_course.Notes);
                    NotificationsCheckBox.IsChecked = _course.EnableNotifications;
                    var list = await _db.GetAssessmentsAsync(_course.CourseId);
                    Assessments.Clear();
                    foreach (var a in list) Assessments.Add(a);
                }
            }
        }

        private void OnSaveClicked(object sender, EventArgs e) => _ = SaveCourseAsync();

        private async Task SaveCourseAsync()
        {
            _course ??= new Course();
            if (_termId.HasValue) _course.TermId = _termId.Value;
            if (string.IsNullOrWhiteSpace(CourseTitle.Text))
            {
                await DisplayAlert("Validation", "Course title is required.", "OK");
                return;
            }

            var selectedStart = new DateTime(
                StartDate.Date.Year,
                StartDate.Date.Month,
                StartDate.Date.Day,
                StartTime.Time.Hours,
                StartTime.Time.Minutes,
                0
            );
            var selectedEnd = new DateTime(
                EndDate.Date.Year,
                EndDate.Date.Month,
                EndDate.Date.Day,
                EndTime.Time.Hours,
                EndTime.Time.Minutes,
                0
            );
            if (selectedStart > selectedEnd)
            {
                await DisplayAlert("Validation", "Start date must be before end date.", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(InstructorName.Text)
                || string.IsNullOrWhiteSpace(InstructorPhone.Text)
                || string.IsNullOrWhiteSpace(InstructorEmail.Text))
            {
                await DisplayAlert("Validation", "Instructor name, phone, and email are required.", "OK");
                return;
            }

            if (!Regex.IsMatch(InstructorEmail.Text, @"^[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}$"))
            {
                await DisplayAlert("Validation", "Please enter a valid email address.", "OK");
                return;
            }

            if (!Regex.IsMatch(InstructorPhone.Text, @"^(?=(?:\D*\d){7,15}\D*$)\+?[\d\s\(\)\-]*$"))
            {
                await DisplayAlert("Validation", "Please enter a valid phone number.", "OK");
                return;
            }

            if (StatusPicker.SelectedIndex < 0)
            {
                await DisplayAlert("Validation", "Course status is required.", "OK");
                return;
            }

            var existing = await _db.GetCoursesAsync(_course.TermId);
            if (existing.Any(x => x.Title != null
                                   && x.Title.Equals(CourseTitle.Text, StringComparison.OrdinalIgnoreCase)
                                   && x.CourseId != _course.CourseId))
            {
                await DisplayAlert("Validation", "A course with that name already exists.", "OK");
                return;
            }

            if (_termId.HasValue)
            {
                var term = await _db.GetTermAsync(_termId.Value);
                if (term != null)
                {
                    if (selectedStart < term.StartDate)
                    {
                        await DisplayAlert("Validation", "Course start date must be on or after the term start date.",
                            "OK");
                        return;
                    }

                    if (selectedEnd.Date > term.EndDate.Date)
                    {
                        await DisplayAlert("Validation", "Course end date must be on or before the term end date.",
                            "OK");
                        return;
                    }
                }
            }

            var now = DateTime.Now;
            var chosenStatusRaw = StatusPicker.SelectedItem?.ToString() ?? "";
            var chosenStatus = chosenStatusRaw.ToUpperInvariant();
            switch (chosenStatus)
            {
                case "IN PROGRESS" when selectedEnd < now:
                    await DisplayAlert("Validation",
                        "You cannot select 'In Progress' unless the end date is on or after today.", "OK");
                    return;
                case "COMPLETED" when selectedEnd > now:
                    await DisplayAlert("Validation",
                        "You cannot select 'Completed' unless the end date is on or before today.", "OK");
                    return;
                case "PLAN TO TAKE" when selectedStart < now:
                    await DisplayAlert("Validation",
                        "You cannot select 'Plan To Take' unless the start date is on or after today.", "OK");
                    return;
            }

            _course.Title = CourseTitle.Text;
            _course.StartDate = selectedStart;
            _course.EndDate = selectedEnd;
            _course.Status = chosenStatusRaw;
            _course.InstructorName = InstructorName.Text;
            _course.InstructorPhone = InstructorPhone.Text;
            _course.InstructorEmail = InstructorEmail.Text;
            _course.Notes = HttpUtility.HtmlEncode(NotesEditor.Text);
            _course.EnableNotifications = NotificationsCheckBox.IsChecked;
            await _db.SaveCourseAsync(_course);
            await HandleCourseNotifications(_course);
            if (Navigation.NavigationStack.Count > 1)
            {
                await Navigation.PopAsync();
            }
        }

        private static async Task HandleCourseNotifications(Course course)
        {
            if (course.EnableNotifications)
            {
                await PushPastNotifications(course);
                await PushFutureNotifications(course);
            }
            else
            {
                await CancelCourseNotifications(course.CourseId);
            }
        }

        private static async Task CancelCourseNotifications(int courseId)
        {
            LocalNotificationCenter.Current.Cancel(GetPastStartNotificationId(courseId));
            LocalNotificationCenter.Current.Cancel(GetPastEndNotificationId(courseId));
            LocalNotificationCenter.Current.Cancel(GetFutureStartNotificationId(courseId));
            LocalNotificationCenter.Current.Cancel(GetFutureEndNotificationId(courseId));
            await Task.CompletedTask;
        }

        private void OnDeleteClicked(object sender, EventArgs e) => _ = DeleteCourseAsync();

        private async Task DeleteCourseAsync()
        {
            if (_course == null) return;
            var confirm = await DisplayAlert("Confirm", $"Delete course '{_course.Title}'?", "Yes", "No");
            if (!confirm) return;
            await CancelCourseNotifications(_course.CourseId);
            await _db.DeleteCourseAsync(_course);
            if (Navigation.NavigationStack.Count > 1)
            {
                await Navigation.PopAsync();
            }
        }

        private void OnCancelClicked(object sender, EventArgs e) => _ = CancelCourseAsync();

        private async Task CancelCourseAsync()
        {
            if (Navigation.NavigationStack.Count > 1)
            {
                await Navigation.PopAsync();
            }
        }

        private void OnShareClicked(object sender, EventArgs e)
        {
            _ = OnShareClickedAsync();
        }

        private async Task OnShareClickedAsync()
        {
            if (string.IsNullOrWhiteSpace(NotesEditor.Text))
            {
                await DisplayAlert("No Notes", "No optional notes to share.", "OK");
                return;
            }

            await Share.Default.RequestAsync(new ShareTextRequest
            {
                Text = NotesEditor.Text,
                Title = "Share Notes"
            });
        }

        private void OnViewAssessmentClicked(object sender, EventArgs e) => _ = ViewAssessmentAsync(sender);

        private async Task ViewAssessmentAsync(object sender)
        {
            if (sender is Button { CommandParameter: int aId } && _course != null)
            {
                await Navigation.PushAsync(new AssessmentDetailPage(aId, _course.CourseId, _db));
            }
        }

        private void OnAddAssessmentClicked(object sender, EventArgs e) => _ = AddAssessmentAsync();

        private async Task AddAssessmentAsync()
        {
            if (_course == null || _course.CourseId == 0)
            {
                await DisplayAlert("Info", "Please save the course before attempting to add an assessment.", "OK");
                return;
            }

            var objectiveCount = Assessments.Count(a => a.Type == "OBJECTIVE");
            var performanceCount = Assessments.Count(a => a.Type == "PERFORMANCE");
            if (Assessments.Count >= 2)
            {
                await DisplayAlert("Limit Reached", "You already have 2 assessments for this course.", "OK");
                return;
            }

            if (objectiveCount >= 1 && performanceCount >= 1)
            {
                await DisplayAlert("Limit Reached", "Only one objective and one performance assessment allowed.", "OK");
                return;
            }

            await Navigation.PushAsync(new AssessmentDetailPage(null, _course.CourseId, _db));
        }

        private static int GetPastStartNotificationId(int courseId) => courseId * 100 + 1;
        private static int GetPastEndNotificationId(int courseId) => courseId * 100 + 2;
        private static int GetFutureStartNotificationId(int courseId) => courseId * 100 + 3;
        private static int GetFutureEndNotificationId(int courseId) => courseId * 100 + 4;

        private static async Task PushPastNotifications(Course c)
        {
            if (!await LocalNotificationCenter.Current.AreNotificationsEnabled())
            {
                await LocalNotificationCenter.Current.RequestNotificationPermission();
            }

            var now = DateTime.Now;
            if (c.StartDate < now)
            {
                var startPast = new NotificationRequest
                {
                    NotificationId = GetPastStartNotificationId(c.CourseId),
                    CategoryType = NotificationCategoryType.Status,
                    Title = "Course Start",
                    Description = $"'{c.Title}' started on {c.StartDate:MMMM d, yyyy 'at' h:mm tt}.",
                    Schedule = new NotificationRequestSchedule { NotifyTime = DateTime.Now }
                };
                await LocalNotificationCenter.Current.Show(startPast);
            }

            if (c.EndDate < now)
            {
                var endPast = new NotificationRequest
                {
                    NotificationId = GetPastEndNotificationId(c.CourseId),
                    CategoryType = NotificationCategoryType.Status,
                    Title = "Course End",
                    Description = $"'{c.Title}' ended on {c.EndDate:MMMM d, yyyy 'at' h:mm tt}.",
                    Schedule = new NotificationRequestSchedule { NotifyTime = DateTime.Now.AddSeconds(2) }
                };
                await LocalNotificationCenter.Current.Show(endPast);
            }
        }

        private static async Task PushFutureNotifications(Course c)
        {
            if (!await LocalNotificationCenter.Current.AreNotificationsEnabled())
            {
                await LocalNotificationCenter.Current.RequestNotificationPermission();
            }

            var now = DateTime.Now;
            if (c.StartDate > now)
            {
                var startFuture = new NotificationRequest
                {
                    NotificationId = GetFutureStartNotificationId(c.CourseId),
                    CategoryType = NotificationCategoryType.Reminder,
                    Title = "Course Start",
                    Description = $"'{c.Title}' starts on {c.StartDate:MMMM d, yyyy 'at' h:mm tt}.",
                    Schedule = new NotificationRequestSchedule { NotifyTime = DateTime.Now }
                };
                await LocalNotificationCenter.Current.Show(startFuture);
            }

            if (c.EndDate > now)
            {
                var endFuture = new NotificationRequest
                {
                    NotificationId = GetFutureEndNotificationId(c.CourseId),
                    CategoryType = NotificationCategoryType.Reminder,
                    Title = "Course End",
                    Description = $"'{c.Title}' ends on {c.EndDate:MMMM d, yyyy 'at' h:mm tt}.",
                    Schedule = new NotificationRequestSchedule { NotifyTime = DateTime.Now.AddSeconds(2) }
                };
                await LocalNotificationCenter.Current.Show(endFuture);
            }
        }
        private async void OnFooterTermsClicked(object sender, EventArgs e)
        {
            try
            {
                await Navigation.PopToRootAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
        }

        private async void OnFooterCoursesClicked(object sender, EventArgs e)
        {
            try
            {
                await DisplayAlert("Info", "You are already viewing this course.", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
        }
    }
}