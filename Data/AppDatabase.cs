using SQLite;
using C971.Models;

namespace C971.Data
{
    public class AppDatabase
    {
        private SQLiteAsyncConnection _db = null!;
        private bool _initialized;

        private async Task Init()
        {
            if (_initialized) return;
            var path = Path.Combine(FileSystem.AppDataDirectory, "C971.db3");
            _db = new SQLiteAsyncConnection(
                path,
                SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache
            );
            await _db.CreateTableAsync<Term>();
            await _db.CreateTableAsync<Course>();
            await _db.CreateTableAsync<Assessment>();
            _initialized = true;
        }

        public async Task SeedAsync()
        {
            await Init();

            if (await _db.Table<Term>().CountAsync() > 0)
            {
                return;
            }

            var t = new Term
            {
                Title = "Spring 2025",
                StartDate = new DateTime(2025, 2, 24),
                EndDate = new DateTime(2025, 2, 28)
            };
            await _db.InsertAsync(t);

            var c = new Course
            {
                TermId = t.TermId,
                Title = "Mobile App Development",
                StartDate = new DateTime(2025, 2, 24),
                EndDate = new DateTime(2025, 2, 28),
                Status = "IN PROGRESS",
                InstructorName = "Anika Patel",
                InstructorPhone = "555-123-4567",
                InstructorEmail = "anika.patel@strimeuniversity.edu",
                EnableNotifications = true
            };
            await _db.InsertAsync(c);

            var a1 = new Assessment
            {
                CourseId = c.CourseId,
                Title = "Objective Assessment",
                Type = "OBJECTIVE",
                StartDate = new DateTime(2025, 2, 24),
                EndDate = new DateTime(2025, 2, 28),
                EnableNotifications = true
            };
            await _db.InsertAsync(a1);

            var a2 = new Assessment
            {
                CourseId = c.CourseId,
                Title = "Performance Assessment",
                Type = "PERFORMANCE",
                StartDate = new DateTime(2025, 2, 24),
                EndDate = new DateTime(2025, 2, 28),
                EnableNotifications = true
            };
            await _db.InsertAsync(a2);
        }

        public async Task<List<Term>> GetTermsAsync()
        {
            await Init();
            return await _db.Table<Term>().ToListAsync();
        }

        public async Task<Term?> GetTermAsync(int id)
        {
            await Init();
            return await _db.Table<Term>().FirstOrDefaultAsync(x => x.TermId == id);
        }

        public async Task SaveTermAsync(Term term)
        {
            await Init();
            if (term.TermId == 0) await _db.InsertAsync(term);
            else await _db.UpdateAsync(term);
        }

        public async Task DeleteTermAsync(Term term)
        {
            await Init();
            await _db.DeleteAsync(term);
        }

        public async Task<List<Course>> GetCoursesAsync(int termId)
        {
            await Init();
            return await _db.Table<Course>().Where(c => c.TermId == termId).ToListAsync();
        }

        public async Task<Course?> GetCourseAsync(int id)
        {
            await Init();
            return await _db.Table<Course>().FirstOrDefaultAsync(x => x.CourseId == id);
        }

        public async Task SaveCourseAsync(Course course)
        {
            await Init();
            if (course.CourseId == 0) await _db.InsertAsync(course);
            else await _db.UpdateAsync(course);
        }

        public async Task DeleteCourseAsync(Course course)
        {
            await Init();
            await _db.DeleteAsync(course);
        }

        public async Task<List<Assessment>> GetAssessmentsAsync(int courseId)
        {
            await Init();
            return await _db.Table<Assessment>().Where(a => a.CourseId == courseId).ToListAsync();
        }

        public async Task<Assessment?> GetAssessmentAsync(int assessmentId)
        {
            await Init();
            return await _db.Table<Assessment>().FirstOrDefaultAsync(a => a.AssessmentId == assessmentId);
        }

        public async Task SaveAssessmentAsync(Assessment assessment)
        {
            await Init();
            if (assessment.AssessmentId == 0) await _db.InsertAsync(assessment);
            else await _db.UpdateAsync(assessment);
        }

        public async Task DeleteAssessmentAsync(Assessment assessment)
        {
            await Init();
            await _db.DeleteAsync(assessment);
        }
    }
}