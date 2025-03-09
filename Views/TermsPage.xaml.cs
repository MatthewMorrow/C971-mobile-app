using System.Collections.ObjectModel;
using C971.Data;
using C971.Models;

namespace C971.Views
{
    public partial class TermsPage : ContentPage
    {
        private readonly AppDatabase _db;
        public ObservableCollection<Term> Terms { get; } = [];

        public TermsPage(AppDatabase database)
        {
            InitializeComponent();
            _db = database;
            TermsCollection.ItemsSource = Terms;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _ = LoadTermsAsync();
        }

        private async Task LoadTermsAsync()
        {
            try
            {
                await _db.SeedAsync();
                Terms.Clear();
                var list = await _db.GetTermsAsync();
                foreach (var t in list)
                    Terms.Add(t);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load terms: {ex.Message}", "OK");
            }
        }

        private void OnAddTermClicked(object sender, EventArgs e) => _ = AddTermAsync();
        private async Task AddTermAsync()
        {
            try
            {
                await Navigation.PushAsync(new TermDetailPage(null, _db));
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Navigation failed: {ex.Message}", "OK");
            }
        }

        private void OnEditTermClicked(object sender, EventArgs e) => _ = EditTermAsync(sender);
        private async Task EditTermAsync(object sender)
        {
            try
            {
                if (sender is Button { CommandParameter: int termId })
                    await Navigation.PushAsync(new TermDetailPage(termId, _db));
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to edit term: {ex.Message}", "OK");
            }
        }

        private void OnDeleteTermClicked(object sender, EventArgs e) => _ = DeleteTermAsync(sender);
        private async Task DeleteTermAsync(object sender)
        {
            try
            {
                if (sender is Button { CommandParameter: int termId })
                {
                    var term = await _db.GetTermAsync(termId);
                    if (term == null) return;
                    var confirm = await DisplayAlert("Confirm", $"Delete term '{term.Title}'?", "Yes", "No");
                    if (!confirm) return;
                    await _db.DeleteTermAsync(term);
                    await LoadTermsAsync();
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to delete term: {ex.Message}", "OK");
            }
        }

        private void OnFooterTermsClicked(object sender, EventArgs e) => _ = FooterTermsAsync();
        private async Task FooterTermsAsync()
        {
            await DisplayAlert("Info", "You are already on the Terms page.", "OK");
        }

        private void OnFooterCoursesClicked(object sender, EventArgs e) => _ = FooterCoursesAsync();
        private async Task FooterCoursesAsync()
        {
            await DisplayAlert("Info", "Select a term to view courses.", "OK");
        }
    }
}
