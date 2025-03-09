using C971.Data;
using C971.Models;
using Plugin.LocalNotification;

namespace C971.Views
{
    public partial class AssessmentDetailPage : ContentPage
    {
        private readonly AppDatabase _db;
        private Assessment? _assessment;
        private readonly int? _assessmentId;
        private readonly int? _courseId;

        public AssessmentDetailPage(int? aid, int? cid, AppDatabase database)
        {
            InitializeComponent();
            _db = database;
            _assessmentId = aid;
            _courseId = cid;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _ = LoadAssessmentAsync();
        }

        private async Task LoadAssessmentAsync()
        {
            if (_assessmentId.HasValue)
            {
                var a = await _db.GetAssessmentAsync(_assessmentId.Value);
                if (a != null)
                {
                    _assessment = a;
                    if (!string.IsNullOrWhiteSpace(_assessment.Title))
                        AssessmentTitle.Text = _assessment.Title;
                    TypePicker.SelectedItem = _assessment.Type;

                    if (_assessment.StartDate != default)
                    {
                        StartDatePicker.Date = _assessment.StartDate.Date;
                        StartTimePicker.Time = _assessment.StartDate.TimeOfDay;
                    }
                    if (_assessment.EndDate != default)
                    {
                        EndDatePicker.Date = _assessment.EndDate.Date;
                        EndTimePicker.Time = _assessment.EndDate.TimeOfDay;
                    }
                    NotificationsCheckBox.IsChecked = _assessment.EnableNotifications;
                }
            }
        }

        private void OnSaveClicked(object sender, EventArgs e) => _ = SaveAssessmentAsync();
        private async Task SaveAssessmentAsync()
        {
            _assessment ??= new Assessment();
            if (_courseId.HasValue) _assessment.CourseId = _courseId.Value;

            if (string.IsNullOrWhiteSpace(AssessmentTitle.Text))
            {
                await DisplayAlert("Validation", "Assessment name is required.", "OK");
                return;
            }
            if (TypePicker.SelectedIndex < 0)
            {
                await DisplayAlert("Validation", "Assessment type is required.", "OK");
                return;
            }

            var selectedStart = new DateTime(
                StartDatePicker.Date.Year,
                StartDatePicker.Date.Month,
                StartDatePicker.Date.Day,
                StartTimePicker.Time.Hours,
                StartTimePicker.Time.Minutes,
                0
            );
            var selectedEnd = new DateTime(
                EndDatePicker.Date.Year,
                EndDatePicker.Date.Month,
                EndDatePicker.Date.Day,
                EndTimePicker.Time.Hours,
                EndTimePicker.Time.Minutes,
                0
            );

            if (selectedStart > selectedEnd)
            {
                await DisplayAlert("Validation", "Start date must be before end date.", "OK");
                return;
            }

            if (_courseId.HasValue)
            {
                var course = await _db.GetCourseAsync(_courseId.Value);
                if (course != null)
                {
                    if (selectedStart < course.StartDate)
                    {
                        await DisplayAlert("Validation", "Assessment start date cannot be before the course start date.", "OK");
                        return;
                    }
                    if (selectedEnd > course.EndDate)
                    {
                        await DisplayAlert("Validation", "Assessment end date cannot be after the course end date.", "OK");
                        return;
                    }
                }
            }


            var chosenType = TypePicker.SelectedItem?.ToString() ?? "";
            var allForCourse = await _db.GetAssessmentsAsync(_assessment.CourseId);
            var objectiveExists = allForCourse.Any(a => a.Type == "OBJECTIVE" && a.AssessmentId != _assessment.AssessmentId);
            var performanceExists = allForCourse.Any(a => a.Type == "PERFORMANCE" && a.AssessmentId != _assessment.AssessmentId);
            var nameConflict = allForCourse.Any(a => a.Title != null
                                                    && a.Title.Equals(AssessmentTitle.Text, StringComparison.OrdinalIgnoreCase)
                                                    && a.AssessmentId != _assessment.AssessmentId);

            if (nameConflict)
            {
                await DisplayAlert("Validation", "An assessment with that name already exists.", "OK");
                return;
            }
            switch (chosenType)
            {
                case "OBJECTIVE" when objectiveExists:
                    await DisplayAlert("Validation", "An Objective assessment already exists.", "OK");
                    return;
                case "PERFORMANCE" when performanceExists:
                    await DisplayAlert("Validation", "A Performance assessment already exists.", "OK");
                    return;
                case "OBJECTIVE" when
                    AssessmentTitle.Text.Contains("performance", StringComparison.OrdinalIgnoreCase):
                    await DisplayAlert("Validation", "An Objective assessment cannot be named 'Performance'.", "OK");
                    return;
                case "PERFORMANCE" when
                    AssessmentTitle.Text.Contains("objective", StringComparison.OrdinalIgnoreCase):
                    await DisplayAlert("Validation", "A Performance assessment cannot be named 'Objective'.", "OK");
                    return;
            }

            _assessment.Title = AssessmentTitle.Text;
            _assessment.Type = chosenType;
            _assessment.StartDate = selectedStart;
            _assessment.EndDate = selectedEnd;
            _assessment.EnableNotifications = NotificationsCheckBox.IsChecked;
            await _db.SaveAssessmentAsync(_assessment);
            await HandleAssessmentNotifications(_assessment);

            if (Navigation.NavigationStack.Count > 1)
            {
                await Navigation.PopAsync();
            }
        }

        private static int GetPastStartNotificationId(int assessmentId) => assessmentId * 100 + 1;
        private static int GetPastEndNotificationId(int assessmentId) => assessmentId * 100 + 2;
        private static int GetFutureStartNotificationId(int assessmentId) => assessmentId * 100 + 3;
        private static int GetFutureEndNotificationId(int assessmentId) => assessmentId * 100 + 4;

        private async Task HandleAssessmentNotifications(Assessment assessment)
        {
            if (assessment.EnableNotifications)
            {
                var course = await _db.GetCourseAsync(assessment.CourseId);
                var courseTitle = course?.Title ?? "Unknown Course";

                await PushPastNotifications(assessment, courseTitle);
                await PushFutureNotifications(assessment, courseTitle);
            }
            else
            {
                await CancelAssessmentNotifications(assessment.AssessmentId);
            }
        }

        private static async Task CancelAssessmentNotifications(int assessmentId)
        {
            LocalNotificationCenter.Current.Cancel(GetPastStartNotificationId(assessmentId));
            LocalNotificationCenter.Current.Cancel(GetPastEndNotificationId(assessmentId));
            LocalNotificationCenter.Current.Cancel(GetFutureStartNotificationId(assessmentId));
            LocalNotificationCenter.Current.Cancel(GetFutureEndNotificationId(assessmentId));
            await Task.CompletedTask;
        }

        private static async Task PushPastNotifications(Assessment a, string courseTitle)
        {
            if (!await LocalNotificationCenter.Current.AreNotificationsEnabled())
            {
                await LocalNotificationCenter.Current.RequestNotificationPermission();
            }

            var now = DateTime.Now;

            if (a.StartDate < now)
            {
                var startPast = new NotificationRequest
                {
                    NotificationId = GetPastStartNotificationId(a.AssessmentId),
                    CategoryType = NotificationCategoryType.Status,
                    Title = "Assessment Start",
                    Description = $"'{courseTitle} - {a.Title}' started on {a.StartDate:MMMM d, yyyy 'at' h:mm tt}.",
                    Schedule = new NotificationRequestSchedule { NotifyTime = DateTime.Now }
                };
                await LocalNotificationCenter.Current.Show(startPast);
            }

            if (a.EndDate < now)
            {
                var endPast = new NotificationRequest
                {
                    NotificationId = GetPastEndNotificationId(a.AssessmentId),
                    CategoryType = NotificationCategoryType.Status,
                    Title = "Assessment End",
                    Description = $"'{courseTitle} - {a.Title}' ended on {a.EndDate:MMMM d, yyyy 'at' h:mm tt}.",
                    Schedule = new NotificationRequestSchedule { NotifyTime = DateTime.Now.AddSeconds(2) }
                };
                await LocalNotificationCenter.Current.Show(endPast);
            }
        }


        private static async Task PushFutureNotifications(Assessment a, string courseTitle)
        {
            if (!await LocalNotificationCenter.Current.AreNotificationsEnabled())
            {
                await LocalNotificationCenter.Current.RequestNotificationPermission();
            }

            var now = DateTime.Now;

            if (a.StartDate > now)
            {
                var futureStart = new NotificationRequest
                {
                    NotificationId = GetFutureStartNotificationId(a.AssessmentId),
                    CategoryType = NotificationCategoryType.Reminder,
                    Title = "Assessment Start",
                    Description = $"'{courseTitle} - {a.Title}' starts on {a.StartDate:MMMM d, yyyy 'at' h:mm tt}.",
                    Schedule = new NotificationRequestSchedule { NotifyTime = DateTime.Now }
                };
                await LocalNotificationCenter.Current.Show(futureStart);
            }

            if (a.EndDate > now)
            {
                var futureEnd = new NotificationRequest
                {
                    NotificationId = GetFutureEndNotificationId(a.AssessmentId),
                    CategoryType = NotificationCategoryType.Reminder,
                    Title = "Assessment End",
                    Description = $"'{courseTitle} - {a.Title}' ends on {a.EndDate:MMMM d, yyyy 'at' h:mm tt}.",
                    Schedule = new NotificationRequestSchedule { NotifyTime = DateTime.Now.AddSeconds(2) }
                };
                await LocalNotificationCenter.Current.Show(futureEnd);
            }
        }

        private void OnDeleteClicked(object sender, EventArgs e) => _ = DeleteAssessmentAsync();
        private async Task DeleteAssessmentAsync()
        {
            if (_assessment == null) return;
            var confirm = await DisplayAlert("Confirm", $"Delete assessment '{_assessment.Title}'?", "Yes", "No");
            if (!confirm) return;
            await _db.DeleteAssessmentAsync(_assessment);
            if (Navigation.NavigationStack.Count > 1)
            {
                await Navigation.PopAsync();
            }
        }

        private void OnCancelClicked(object sender, EventArgs e) => _ = CancelAsync();
        private async Task CancelAsync()
        {
            if (Navigation.NavigationStack.Count > 1)
            {
                await Navigation.PopAsync();
            }
        }

        private void OnFooterTermsClicked(object sender, EventArgs e) => _ = FooterTermsAsync();
        private async Task FooterTermsAsync()
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

        private void OnFooterCoursesClicked(object sender, EventArgs e) => _ = FooterCoursesAsync();
        private async Task FooterCoursesAsync()
        {
            try
            {
                await DisplayAlert("Info", "You are already viewing assessment details.", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
        }
    }
}