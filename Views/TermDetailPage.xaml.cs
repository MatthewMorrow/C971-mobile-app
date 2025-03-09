using System.Collections.ObjectModel;
using C971.Data;
using C971.Models;

namespace C971.Views
{
    public partial class TermDetailPage : ContentPage
    {
        private readonly AppDatabase _db;
        private Term? _term;
        private readonly int? _termId;
        private DateTime _originalStart;
        private DateTime _originalEnd;
        public ObservableCollection<Course> Courses { get; } = [];

        public TermDetailPage(int? id, AppDatabase database)
        {
            InitializeComponent();
            _db = database;
            _termId = id;
            CoursesCollection.ItemsSource = Courses;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _ = LoadTermAsync();
        }

        private async Task LoadTermAsync()
        {
            try
            {
                if (_termId == null)
                {
                    DeleteTermButton.IsEnabled = false;
                    return;
                }
                var term = await _db.GetTermAsync(_termId.Value);
                if (term == null) return;
                _term = term;
                if (!string.IsNullOrWhiteSpace(_term.Title))
                    TermTitle.Text = _term.Title;
                StartDate.Date = _term.StartDate;
                EndDate.Date = _term.EndDate;
                _originalStart = _term.StartDate;
                _originalEnd = _term.EndDate;
                var list = await _db.GetCoursesAsync(_term.TermId);
                Courses.Clear();
                foreach (var c in list)
                    Courses.Add(c);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load term: {ex.Message}", "OK");
            }
        }

        private void OnSaveTermClicked(object sender, EventArgs e) => _ = SaveTermAsync();
        private async Task SaveTermAsync()
        {
            try
            {
                _term ??= new Term();
                if (string.IsNullOrWhiteSpace(TermTitle.Text))
                {
                    await DisplayAlert("Validation", "Term Title is required.", "OK");
                    return;
                }
                if (StartDate.Date > EndDate.Date)
                {
                    await DisplayAlert("Validation", "Start date must be before end date.", "OK");
                    return;
                }
                var existing = await _db.GetTermsAsync();
                if (existing.Any(t => t.Title != null
                                      && t.Title.Equals(TermTitle.Text, StringComparison.OrdinalIgnoreCase)
                                      && t.TermId != _term.TermId))
                {
                    await DisplayAlert("Validation", "A term with that name already exists.", "OK");
                    return;
                }
                _term.Title = TermTitle.Text;
                _term.StartDate = StartDate.Date;
                _term.EndDate = EndDate.Date;
                await _db.SaveTermAsync(_term);
                _originalStart = _term.StartDate;
                _originalEnd = _term.EndDate;
                await Navigation.PopAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to save term: {ex.Message}", "OK");
            }
        }

        private void OnDeleteTermClicked(object sender, EventArgs e) => _ = DeleteTermAsync();
        private async Task DeleteTermAsync()
        {
            try
            {
                if (_term == null) return;
                var confirm = await DisplayAlert("Confirm", $"Delete term '{_term.Title}'?", "Yes", "No");
                if (!confirm) return;
                await _db.DeleteTermAsync(_term);
                await Navigation.PopAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to delete term: {ex.Message}", "OK");
            }
        }

        private void OnCancelTermClicked(object sender, EventArgs e) => _ = CancelTermAsync();
        private async Task CancelTermAsync()
        {
            try
            {
                await Navigation.PopAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to cancel: {ex.Message}", "OK");
            }
        }

        private void OnEditCourseClicked(object sender, EventArgs e) => _ = EditCourseAsync(sender);
        private async Task EditCourseAsync(object sender)
        {
            try
            {
                if (sender is Button { CommandParameter: int courseId })
                {
                    if (_term == null) return;
                    if (StartDate.Date != _originalStart || EndDate.Date != _originalEnd)
                    {
                        await DisplayAlert("Info", "Please save the term before attempting to edit a course.", "OK");
                        return;
                    }
                    await Navigation.PushAsync(new CourseDetailPage(courseId, _term.TermId, _db));
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to edit course: {ex.Message}", "OK");
            }
        }

        private void OnDeleteCourseClicked(object sender, EventArgs e) => _ = DeleteCourseAsync(sender);
        private async Task DeleteCourseAsync(object sender)
        {
            try
            {
                if (sender is Button { CommandParameter: int courseId })
                {
                    var course = await _db.GetCourseAsync(courseId);
                    if (course == null) return;
                    var confirm = await DisplayAlert("Confirm", $"Delete course '{course.Title}'?", "Yes", "No");
                    if (!confirm) return;
                    await _db.DeleteCourseAsync(course);
                    if (_term != null) await LoadTermAsync();
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to delete course: {ex.Message}", "OK");
            }
        }

        private void OnAddCourseClicked(object sender, EventArgs e) => _ = AddCourseAsync();
        private async Task AddCourseAsync()
        {
            try
            {
                if (_term == null || _term.TermId == 0)
                {
                    await DisplayAlert("Info", "Please save the term before attempting to add a course.", "OK");
                    return;
                }
                var currentCourses = await _db.GetCoursesAsync(_term.TermId);
                if (currentCourses.Count >= 6)
                {
                    await DisplayAlert("Limit Reached", "You cannot add more than 6 courses per term.", "OK");
                    return;
                }
                await Navigation.PushAsync(new CourseDetailPage(null, _term.TermId, _db));
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to add course: {ex.Message}", "OK");
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
                await DisplayAlert("Info", "You are already viewing courses for this term.", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
        }
    }
}
