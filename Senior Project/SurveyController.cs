using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Net.Mail;
using FANQAPortal.Models;
using System.Configuration;
using System.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System.IO;
using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.tool.xml;

namespace FANQAPortal.Controllers
{
    public class SurveyController : Controller
    {
        private ApplicationDbContext context;
        DbContextOptionsBuilder<ApplicationDbContext> _optionsBuilder;
        private readonly UserManager<ApplicationUser> _userManager;
        private IEditingInstanceRepository repository_EditInstance;
        private IDomainCommentRepository repository_DomainComment;
        private IDomainRepository repository_Domain;
        private ITopicRepository repository_Topic;
        private IQuestionRepository repository_Question;
        private ISurveyRepository repository_Survey;
        private IProgramUserRepository repository_ProgramUser;
        private ISurveyInstanceRepository repository_SurveyInstance;
        private IProgramRepository repository_Program;
        private SurveyInstanceViewModel ViewModel;
        private static int currentDomainId;

        public SurveyController(ApplicationDbContext context, IDomainRepository domainRepo, ITopicRepository topicRepo, IQuestionRepository questionRepo, ISurveyRepository surveyRepo, IProgramUserRepository proguserRepo, IProgramRepository progRepo, ISurveyInstanceRepository siRepo, UserManager<ApplicationUser> userManager, IEditingInstanceRepository eiRepo, IDomainCommentRepository domainCommentRepo)
        {
            _optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            repository_Domain = domainRepo;
            repository_Topic = topicRepo;
            repository_Question = questionRepo;
            repository_Survey = surveyRepo;
            repository_SurveyInstance = siRepo;
            repository_ProgramUser = proguserRepo;
            repository_Program = progRepo;
            repository_EditInstance = eiRepo;
            repository_DomainComment = domainCommentRepo;
            ViewModel = new SurveyInstanceViewModel(null, null, null, null, null, null, 0, 0);

            _userManager = userManager;

        }

        [HttpPost]
        public IActionResult BeginSurvey(List<Program> Programs, int ProgId, int SurveyId)
        {
            List<SurveyInstance> stagedAnswers = new List<SurveyInstance>();

            if (ProgId == 0)
            {
                for (int i = 0; i < Programs.Count(); i++)
                {
                    var prog = Programs[i];
                    Program program = repository_Program.Programs.FirstOrDefault(p => p.ProgramId == prog.ProgramId);
                    prog.SurveyInProgress = 1;
                    repository_Program.SaveProgram(program);
                    string surveyName = repository_Survey.Surveys.FirstOrDefault(p => p.SurveyId == SurveyId).SurveyName;
                    int surveyInstanceId = repository_SurveyInstance.SurveyAnswers.Max(p => p.SurveyInstanceId) + 1;
                    int surveyAnswerId = repository_SurveyInstance.SurveyAnswers.Last().SurveyAnswerId + 1;

                    foreach (Domain domain in repository_Domain.Domains.Where(p => p.Active == 1 && p.SurveyId == SurveyId).OrderBy(p => p.DomainOrder))
                    {
                        foreach (Topic topic in repository_Topic.Topics.Where(p => p.DomainId == domain.DomainId && p.Active == 1).OrderBy(p => p.TopicOrder))
                        {
                            foreach (Question question in repository_Question.Questions.Where(p => p.TopicId == topic.TopicId && p.Active == 1).OrderBy(p => p.QuestionOrder))
                            {
                                SurveyInstance stage = new SurveyInstance();
                                stage.SurveyAnswerId = surveyAnswerId;
                                stage.SurveyInstanceId = surveyInstanceId;
                                stage.ProgramId = prog.ProgramId;
                                stage.QuestionId = question.QuestionId;
                                stage.inProgress = 1;
                                stage.Answer = -1;
                                stagedAnswers.Add(stage);

                                surveyAnswerId++;
                            }
                        }
                    }
                    program.SurveyInProgress = 1;

                    repository_SurveyInstance.AddSurvey(stagedAnswers);

                    stagedAnswers.Clear();
                }
            }

            else
            {
                Program program = repository_Program.Programs.FirstOrDefault(p => p.ProgramId == ProgId);
                for (int i = 0; i < Programs.Count(); i++)
                {
                    if (Programs[i].ProgramId == ProgId)
                    {
                        Programs[i].SurveyInProgress = 1;
                        break;
                    }
                }
                // Programs.RemoveAt(Programs.Find(p => p.ProgramId == ProgId));
                string surveyName = repository_Survey.Surveys.FirstOrDefault(p => p.SurveyId == SurveyId).SurveyName;
                int surveyInstanceId = 0;
                int surveyAnswerId = 0;

                if (repository_SurveyInstance.SurveyAnswers.Count() == 0)
                {
                    surveyAnswerId = 1;
                    surveyInstanceId = 1;
                }
                else
                {
                    surveyAnswerId = repository_SurveyInstance.SurveyAnswers.Last().SurveyAnswerId + 1;
                    surveyInstanceId = repository_SurveyInstance.SurveyAnswers.Max(p => p.SurveyInstanceId) + 1;
                }

                foreach (Domain domain in repository_Domain.Domains.Where(p => p.Active == 1 && p.SurveyId == SurveyId).OrderBy(p => p.DomainOrder))
                {
                    foreach (Topic topic in repository_Topic.Topics.Where(p => p.DomainId == domain.DomainId && p.Active == 1).OrderBy(p => p.TopicOrder))
                    {
                        foreach (Question question in repository_Question.Questions.Where(p => p.TopicId == topic.TopicId && p.Active == 1).OrderBy(p => p.QuestionOrder))
                        {
                            SurveyInstance stage = new SurveyInstance();
                            stage.SurveyAnswerId = surveyAnswerId;
                            stage.SurveyInstanceId = surveyInstanceId;
                            stage.ProgramId = program.ProgramId;
                            stage.QuestionId = question.QuestionId;
                            stage.inProgress = 1;
                            stage.Answer = -1;
                            stagedAnswers.Add(stage);

                            surveyAnswerId++;
                        }
                    }
                }

                program.SurveyInProgress = 1;
                repository_Program.SaveProgram(program);

                repository_SurveyInstance.AddSurvey(stagedAnswers);
            }
            ProgId = 0;
            int surveyId = repository_Survey.Surveys.Min(p => p.SurveyId);

            return View("StartSurvey", new ProgramListViewModel(ProgId, Programs, surveyId, repository_Survey.Surveys.Where(p => p.Open == 1).ToList()));
        }

        public async Task<IActionResult> StartSurvey()
        {
            var currentUser = await _userManager.GetUserAsync(HttpContext.User);
            string id = currentUser.Id;
            List<Program> programs = new List<Program>();
            foreach (ProgramUser user in repository_ProgramUser.ProgramUsers.Where(p => p.UserId == id))
            {
                Program program = repository_Program.Programs.First(p => user.ProgramId == p.ProgramId);
                programs.Add(program);
            }
            return View("StartSurvey", new ProgramListViewModel(0, programs, repository_Survey.Surveys.Min(p => p.SurveyId), repository_Survey.Surveys.Where(p => p.Open == 1).ToList()));
        }

        public ViewResult SubmitSurvey(int currentProgramId)
        {
            var userId = _userManager.GetUserId(HttpContext.User);
            var progusers = repository_ProgramUser.ProgramUsers.ToList();
            var currentuser = progusers.FirstOrDefault(p => p.UserId == userId && p.ProgramId == currentProgramId);
            var commentList = repository_DomainComment.DomainComments.Where(p => p.SurveyInstanceId == currentProgramId).ToList();
            var list = repository_SurveyInstance.SurveyAnswers.Where(p => p.ProgramId == currentProgramId && p.inProgress == 1).ToList();
            Program program = repository_Program.Programs.First(p => currentProgramId == p.ProgramId);
            var UserProgramList = progusers.Where(q => q.UserId == userId && q.ApprovalStatus == 1);

            for (int i = 0; i < list.Count(); i++)
            {
                list[i].inProgress = 0;
                list[i].SubmittedBy = currentuser.FirstName + " " + currentuser.LastName;
                list[i].SubmissionDate = DateTime.Now;
            }
            repository_SurveyInstance.SubmitSurvey(list);

            var EditInstanceList = repository_EditInstance.EditingInstances.Where(p => list.FirstOrDefault(q => q.SurveyAnswerId == p.SurveyAnswerId) != default).ToList();
            program.SurveyInProgress = 0;



            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            repository_EditInstance.DeleteEdits(EditInstanceList);




            repository_Program.SaveProgram(program);
            var ProgramList = repository_Program.Programs.Where(p => UserProgramList.FirstOrDefault(q => p.ProgramId == q.ProgramId) != default).ToList();
            var AnswerList = (from sa in repository_SurveyInstance.SurveyAnswers
                            join q in repository_Question.Questions
                            on sa.QuestionId equals q.QuestionId
                            join t in repository_Topic.Topics
                            on q.TopicId equals t.TopicId
                            join d in repository_Domain.Domains
                            on t.DomainId equals d.DomainId
                            join s in repository_Survey.Surveys
                            on d.SurveyId equals s.SurveyId
                            join p in repository_Program.Programs
                            on sa.ProgramId equals p.ProgramId
                            join pu in repository_ProgramUser.ProgramUsers
                            on p.ProgramId equals pu.ProgramId
                            where sa.inProgress == 0 && pu.UserId == userId && pu.ApprovalStatus == 1
                            orderby d.DomainOrder, t.TopicOrder, q.QuestionOrder

                            select new QuestionInfo
                            {
                                QuestionId = q.QuestionId,
                                QuestionOrder = q.QuestionOrder,
                                SurveyAnswerId = sa.SurveyAnswerId,
                                TopicId = t.TopicId,
                                TopicOrder = t.TopicOrder,
                                TopicName = t.TopicName,
                                DomainId = d.DomainId,
                                DomainOrder = d.DomainOrder,
                                DomainName = d.DomainName,
                                QuestionPrompt = q.QuestionPrompt,
                                SurveyId = s.SurveyId,
                                SurveyName = s.SurveyName,
                                ProgramId = sa.ProgramId,
                                SurveyInstanceId = sa.SurveyInstanceId,
                                SubmissionDate = sa.SubmissionDate,
                                Answer = sa.Answer

                            }).ToList();
            return View("SurveyHistory", new SurveyHistoryViewModel(AnswerList, ProgramList));
        }
        public ViewResult StressTest(int currentProgramId)
        {
            var userId = _userManager.GetUserId(HttpContext.User);
            
            Random rnd = new Random();
            int SurveyAmount = 200;
            int count = repository_Program.Programs.Count();
            var ProgList = repository_Program.Programs.ToList();
            int surveyInstanceId = repository_SurveyInstance.SurveyAnswers.Max(p => p.SurveyInstanceId) + 1;
            int surveyAnswerId = repository_SurveyInstance.SurveyAnswers.Last().SurveyAnswerId + 1;
            List<SurveyInstance> surveyCluster = new List<SurveyInstance>();

            for (int a = 0; a < SurveyAmount; a++)
            {
                var surveys = repository_Survey.Surveys.Where(p => p.Open == 1).ToList();
                 
                int winner = rnd.Next(0, count);
                int ProgramId = ProgList.ElementAt(winner).ProgramId;
                var UserProgramList = repository_ProgramUser.ProgramUsers.Where(q => q.ProgramId == ProgramId && q.RoleName == "ProgramAdmin").ToList();


                for (int b = 0; b < surveys.Count(); b++)
                {
                    var domains = repository_Domain.Domains.Where(p => p.Active == 1 && p.SurveyId == surveys[b].SurveyId).OrderBy(p => p.DomainOrder).ToList();
                    for (int c = 0; c < domains.Count(); c++)
                    {
                        var topics = repository_Topic.Topics.Where(p => p.DomainId == domains[c].DomainId && p.Active == 1).OrderBy(p => p.TopicOrder).ToList();
                        for (int d = 0; d < topics.Count(); d++)
                        {
                            var questions = repository_Question.Questions.Where(p => p.TopicId == topics[d].TopicId && p.Active == 1).OrderBy(p => p.QuestionOrder).ToList();
                            string name = "";
                            if (UserProgramList.Count() == 0)
                            {
                                name = "John Doe";
                            }
                            else
                            {
                                name = UserProgramList.First().FirstName + " " + UserProgramList.First().LastName;
                            }
                            for (int e = 0; e < questions.Count(); e++)
                            {
                                SurveyInstance stage = new SurveyInstance();
                                stage.SurveyAnswerId = surveyAnswerId;
                                stage.SurveyInstanceId = surveyInstanceId;
                                stage.ProgramId = ProgramId;
                                stage.QuestionId = questions[e].QuestionId;
                                stage.Answer = rnd.Next(0, 6);
                                stage.AnswerText = "This is just a test.";
                                stage.SubmittedBy = name;
                                stage.SubmissionDate = DateTime.Now;
                                stage.inProgress = 0;
                                surveyCluster.Add(stage);

                                surveyAnswerId++;
                            }
                        }
                    }
                }
                surveyInstanceId++;
                if(a % 20 == 19)
                {
                    repository_SurveyInstance.AddSurvey(surveyCluster);
                    surveyCluster.Clear();
                }
            }
            return View("Index");
        }

        public IActionResult CompleteDomain(int ProgId)
        {
            ViewBag.ProgramName = repository_Program.Programs.FirstOrDefault(p => p.ProgramId == ProgId).ProgramName;
            var instance = (from sa in repository_SurveyInstance.SurveyAnswers
                            join q in repository_Question.Questions
                            on sa.QuestionId equals q.QuestionId
                            join t in repository_Topic.Topics
                            on q.TopicId equals t.TopicId
                            join d in repository_Domain.Domains
                            on t.DomainId equals d.DomainId
                            join s in repository_Survey.Surveys
                            on d.SurveyId equals s.SurveyId
                            where sa.inProgress == 1 && sa.ProgramId == ProgId
                            orderby d.DomainOrder, t.TopicOrder, q.QuestionOrder

                            select new QuestionInfo
                            {
                                QuestionId = q.QuestionId,
                                QuestionOrder = q.QuestionOrder,
                                SurveyAnswerId = sa.SurveyAnswerId,
                                SurveyInstanceId = sa.SurveyInstanceId,
                                TopicId = t.TopicId,
                                TopicOrder = t.TopicOrder,
                                TopicName = t.TopicName,
                                DomainId = d.DomainId,
                                DomainOrder = d.DomainOrder,
                                DomainName = d.DomainName,
                                QuestionPrompt = q.QuestionPrompt,
                                SurveyId = s.SurveyId,
                                SurveyName = s.SurveyName,
                                ProgramId = sa.ProgramId,
                                Answer = sa.Answer

                            }).ToList();

            return View(instance);
        }

        public IActionResult CompleteSurvey(int domainId, int ProgId)
        {
            ViewBag.ProgId = ProgId;
            if (domainId != 0)
            {
                currentDomainId = domainId;
                ViewModel.Domain = repository_Domain.Domains.First(p => p.DomainId == domainId);
            }
            else
            {
                ViewModel.Domain = repository_Domain.Domains.First(p => p.DomainId == currentDomainId);
            }
            var questionList = repository_Question.Questions.Where(p => p.Active == 1).ToList();

            var InstanceAnswers = (from sa in repository_SurveyInstance.SurveyAnswers
                                   join q in repository_Question.Questions
                                   on sa.QuestionId equals q.QuestionId
                                   join t in repository_Topic.Topics
                                   on q.TopicId equals t.TopicId
                                   join d in repository_Domain.Domains
                                   on t.DomainId equals d.DomainId
                                   join s in repository_Survey.Surveys
                                   on d.SurveyId equals s.SurveyId
                                   where sa.inProgress == 1 && s.SurveyId == d.SurveyId && q.Active == 1 && t.Active == 1 && sa.ProgramId == ProgId
                                   orderby d.DomainOrder, t.TopicOrder, q.QuestionOrder

                                   select new QuestionInfo
                                   {
                                       QuestionId = q.QuestionId,
                                       QuestionOrder = q.QuestionOrder,
                                       SurveyAnswerId = sa.SurveyAnswerId,
                                       TopicId = t.TopicId,
                                       TopicOrder = t.TopicOrder,
                                       TopicName = t.TopicName,
                                       DomainId = d.DomainId,
                                       DomainOrder = d.DomainOrder,
                                       DomainName = d.DomainName,
                                       QuestionPrompt = q.QuestionPrompt,
                                       SurveyId = s.SurveyId,
                                       SurveyName = s.SurveyName,
                                       ProgramId = sa.ProgramId,
                                       SurveyInstanceId = sa.SurveyInstanceId,
                                       Answer = sa.Answer

                                   }).ToList();

            ViewModel.SurveyAnswers = InstanceAnswers;
            ViewModel.Domains = InstanceAnswers.Distinct(new QuestionInfoDomainComparer()).ToList();
            ViewModel.Topics = InstanceAnswers.Distinct(new QuestionInfoTopicComparer()).ToList();
            ViewModel.Questions = InstanceAnswers;
            ViewModel.DomainComments = (from c in repository_DomainComment.DomainComments
                                        join sa in repository_SurveyInstance.SurveyAnswers
                                        on c.SurveyInstanceId equals sa.SurveyInstanceId
                                        join d in repository_Domain.Domains
                                        on c.DomainId equals d.DomainId
                                        join s in repository_Survey.Surveys
                                        on d.SurveyId equals s.SurveyId
                                        where ProgId == sa.ProgramId && d.DomainId == ViewModel.Domain.DomainId && sa.inProgress == 1
                                        orderby d.DomainOrder

                                        select new CommentInstance
                                        {
                                            Comment = c.Comment,
                                            DomainCommentId = c.DomainCommentId,
                                            ProgramId = sa.ProgramId,
                                            DomainId = c.DomainId,
                                            SurveyId = s.SurveyId,
                                            SurveyInstanceId = sa.SurveyInstanceId,
                                            TimeOfComment = c.TimeOfComment,
                                            UpdatedBy = c.UpdatedBy,

                                        }).Distinct(new CommentInstanceCommentComparer()).ToList();

            int max = ViewModel.Domains.ToList().Last().DomainId;
            int min = ViewModel.Domains.ToList().First().DomainId;
            if (domainId != max)
            {
                ViewBag.Next = ViewModel.Domain.DomainId + 1;
            }
            if (domainId != min)
            {
                ViewBag.Previous = ViewModel.Domain.DomainId - 1;
            }
            ViewBag.Max = max;
            ViewBag.Min = min;

            return View(ViewModel);
        }
        public IActionResult SurveyHistory()
        {
            var userId = _userManager.GetUserId(HttpContext.User);
            var UserProgramList = repository_ProgramUser.ProgramUsers.Where(q => q.UserId == userId && q.ApprovalStatus == 1).ToList();

            var ProgramList = repository_Program.Programs.Where(p => UserProgramList.FirstOrDefault(q => p.ProgramId == q.ProgramId) != default).ToList();

            var AnswerList = (from sa in repository_SurveyInstance.SurveyAnswers
                                   join q in repository_Question.Questions
                                   on sa.QuestionId equals q.QuestionId
                                   join t in repository_Topic.Topics
                                   on q.TopicId equals t.TopicId
                                   join d in repository_Domain.Domains
                                   on t.DomainId equals d.DomainId
                                   join s in repository_Survey.Surveys
                                   on d.SurveyId equals s.SurveyId
                                   join p in repository_Program.Programs
                                   on sa.ProgramId equals p.ProgramId
                                   join pu in repository_ProgramUser.ProgramUsers
                                   on p.ProgramId equals pu.ProgramId
                                   where sa.inProgress == 0 && s.SurveyId == d.SurveyId
                                   orderby d.DomainOrder, t.TopicOrder, q.QuestionOrder

                                   select new QuestionInfo
                                   {
                                       QuestionId = q.QuestionId,
                                       QuestionOrder = q.QuestionOrder,
                                       SurveyAnswerId = sa.SurveyAnswerId,
                                       TopicId = t.TopicId,
                                       TopicOrder = t.TopicOrder,
                                       TopicName = t.TopicName,
                                       DomainId = d.DomainId,
                                       DomainOrder = d.DomainOrder,
                                       DomainName = d.DomainName,
                                       QuestionPrompt = q.QuestionPrompt,
                                       SurveyId = s.SurveyId,
                                       SurveyName = s.SurveyName,
                                       ProgramId = sa.ProgramId,
                                       SurveyInstanceId = sa.SurveyInstanceId,
                                       Answer = sa.Answer,
                                       SubmissionDate = sa.SubmissionDate

                                   }).ToList();

            return View(new SurveyHistoryViewModel(AnswerList, ProgramList));
        }
        public IActionResult DomainHistory(int instanceNo, int domainNo)
        {
            var AnswerList = (from sa in repository_SurveyInstance.SurveyAnswers
                                   join q in repository_Question.Questions
                                   on sa.QuestionId equals q.QuestionId
                                   join t in repository_Topic.Topics
                                   on q.TopicId equals t.TopicId
                                   join d in repository_Domain.Domains
                                   on t.DomainId equals d.DomainId
                                   join s in repository_Survey.Surveys
                                   on d.SurveyId equals s.SurveyId
                                   where sa.inProgress == 0 && sa.SurveyInstanceId == instanceNo
                                   orderby d.DomainOrder, t.TopicOrder, q.QuestionOrder

                                   select new QuestionInfo
                                   {
                                       QuestionId = q.QuestionId,
                                       QuestionOrder = q.QuestionOrder,
                                       SurveyAnswerId = sa.SurveyAnswerId,
                                       TopicId = t.TopicId,
                                       TopicOrder = t.TopicOrder,
                                       TopicName = t.TopicName,
                                       DomainId = d.DomainId,
                                       DomainOrder = d.DomainOrder,
                                       DomainName = d.DomainName,
                                       QuestionPrompt = q.QuestionPrompt,
                                       SurveyId = s.SurveyId,
                                       SurveyName = s.SurveyName,
                                       ProgramId = sa.ProgramId,
                                       SurveyInstanceId = sa.SurveyInstanceId,
                                       Answer = sa.Answer,
                                       SubmissionDate = sa.SubmissionDate

                                   }).ToList();

            var Domain = repository_Domain.Domains.FirstOrDefault(p => p.DomainId == domainNo);
            var DomainList = AnswerList.Distinct(new QuestionInfoDomainComparer()).ToList();
            var TopicList = AnswerList.Where(p => p.DomainId == domainNo).Distinct(new QuestionInfoTopicComparer()).ToList();
            var QuestionList = AnswerList.Where(p=> p.DomainId == domainNo).ToList();
            var CommentList = repository_DomainComment.DomainComments.Where(p => p.SurveyInstanceId == instanceNo && p.DomainId == domainNo).ToList();

            ViewBag.DomainName = repository_Domain.Domains.FirstOrDefault(p => p.DomainId == domainNo).DomainName;
            int max = DomainList.Max(p => p.DomainId);
            int min = DomainList.Min(p => p.DomainId);
            if (domainNo != max)
            {
                ViewBag.Next = domainNo + 1;
            }
            if (domainNo != min)
            {
                ViewBag.Previous = domainNo - 1;
            }
            ViewBag.Max = max;
            ViewBag.Min = min;
            DomainHistoryViewModel viewModel = new DomainHistoryViewModel(AnswerList, DomainList, TopicList, QuestionList, CommentList, "-1,0,1,2,3,4,5");
            return View(viewModel);
        }
        public IActionResult PreviousSurvey()
        {
            return View(repository_Domain.Domains);
        }

        [HttpPost]
        public IActionResult UpdateSurvey(int surveyAnswerId, int domainId, int answer, string comment)
        {
            var userId = _userManager.GetUserId(HttpContext.User);
            var UserProgramList = repository_ProgramUser.ProgramUsers.Where(q => q.UserId == userId).ToList();
            SurveyInstance Answer = repository_SurveyInstance.SurveyAnswers.FirstOrDefault(p => p.SurveyAnswerId == surveyAnswerId);

            if (answer == 1)
            {
                Answer.Answer = answer;
                Answer.AnswerText = "Level 1";
            }
            else if (answer == 2)
            {
                Answer.Answer = answer;
                Answer.AnswerText = "Level 2";
            }
            else if (answer == 3)
            {
                Answer.Answer = answer;
                Answer.AnswerText = "Level 3";
            }
            else if (answer == 4)
            {
                Answer.Answer = answer;
                Answer.AnswerText = "Level 4";
            }
            else if (answer == 5)
            {
                Answer.Answer = answer;
                Answer.AnswerText = "Level 5";
            }
            else
            {
                Answer.Answer = answer;
                Answer.AnswerText = "Level 0";
            }
            
            repository_SurveyInstance.SaveSurvey(Answer);
            EditInstance editInstance = new EditInstance();
            if (repository_EditInstance.EditingInstances.Count() != 0)
            {
                editInstance.EditInstanceId = repository_EditInstance.EditingInstances.Max(p => p.EditInstanceId) + 1;
            }
            else
            {
                editInstance.EditInstanceId = 1;
            }
            editInstance.SurveyAnswerId = Answer.SurveyAnswerId;
            editInstance.Answer = Answer.Answer;
            editInstance.AnswerText = Answer.AnswerText;
            editInstance.UpdatedBy = UserProgramList.First().FirstName + ' ' + UserProgramList.First().LastName;
            editInstance.TimeOfEdit = DateTime.Now;
            editInstance.UserId = userId;
            editInstance.UserName = _userManager.GetUserName(HttpContext.User);
            bool exception = true;
            while(exception == true)
            {
                try
                {
                    repository_EditInstance.SaveEditingInstance(editInstance);
                    exception = false;
                }
                catch
                {
                    editInstance.EditInstanceId++;
                    exception = true;
                }
            }
            repository_EditInstance.SaveEditingInstance(editInstance);

            return RedirectToAction("CompleteSurvey", new { domainId = domainId, ProgId = Answer.ProgramId });
        }

        [HttpPost]
        public FileResult GetReport(string filter, int instanceId)
        {
            using MemoryStream stream = new System.IO.MemoryStream();
            string[] filterArray = filter.Split(",");
            List<int> answerList = new List<int>();
            foreach (var filterString in filterArray)
            {
                if(filterString != "")
                {
                    answerList.Add(int.Parse(filterString));
                }
            }
            int[] answers = answerList.ToArray();
            var FilteredReport = (from sa in repository_SurveyInstance.SurveyAnswers
                              join q in repository_Question.Questions
                              on sa.QuestionId equals q.QuestionId
                              join t in repository_Topic.Topics
                              on q.TopicId equals t.TopicId
                              join d in repository_Domain.Domains
                              on t.DomainId equals d.DomainId
                              join s in repository_Survey.Surveys
                              on d.SurveyId equals s.SurveyId
                              where sa.inProgress == 0 && sa.SurveyInstanceId == instanceId && answers.Contains(sa.Answer)
                              orderby d.DomainOrder, t.TopicOrder, q.QuestionOrder

                              select new QuestionInfo
                              {
                                  QuestionId = q.QuestionId,
                                  QuestionOrder = q.QuestionOrder,
                                  SurveyAnswerId = sa.SurveyAnswerId,
                                  TopicId = t.TopicId,
                                  TopicOrder = t.TopicOrder,
                                  TopicName = t.TopicName,
                                  DomainId = d.DomainId,
                                  DomainOrder = d.DomainOrder,
                                  DomainName = d.DomainName,
                                  QuestionPrompt = q.QuestionPrompt,
                                  SurveyId = s.SurveyId,
                                  SurveyName = s.SurveyName,
                                  ProgramId = sa.ProgramId,
                                  SurveyInstanceId = sa.SurveyInstanceId,
                                  Answer = sa.Answer,
                                  SubmissionDate = sa.SubmissionDate

                              }).ToList();

            string reportHtml = $"<h1>{FilteredReport.First().SurveyName} - Submitted On {FilteredReport.First().SubmissionDate}</h1>\n<br/>";
            var DomainList = FilteredReport.Distinct(new QuestionInfoDomainComparer()).ToList();

            foreach (var domain in DomainList)
            {
                reportHtml += $"<h2 style=\"text-align:center\">{domain.DomainName}</h2>\n";
                var TopicList = FilteredReport.Where(p => p.DomainId == domain.DomainId).Distinct(new QuestionInfoTopicComparer()).ToList();

                foreach (var topic in TopicList)
                {
                    reportHtml += "<table style=\"width:700px;margin:auto;\" class=\"table table-bordered table-condensed\">\n";
                    reportHtml += "<tr>\n" +
                                       $"<td style=\"font-weight: bold\">{topic.TopicName}</td>\n" +
                                       "<td style=\"font-weight: bold\">Answers</td>\n" +
                                   "</tr>\n";
                    var QuestionList = FilteredReport.Where(p => p.TopicId == topic.TopicId);

                    foreach(var question in QuestionList)
                    {
                        reportHtml += "<tr>\n" +
                                            $"<td>{(char)(question.QuestionOrder + 96)}. {question.QuestionPrompt}</td>\n" +
                                            $"<td>{question.Answer}</td>\n" +
                                      "</tr>\n";
                    }
                    reportHtml += "\n</table>\n";
                }
                var CommentList = repository_DomainComment.DomainComments.Where(p => p.SurveyInstanceId == instanceId && p.DomainId == domain.DomainId).ToList();
                if (CommentList.Count() == 0)
                {
                    reportHtml += "\n<h3 style=\"text-align:center\">--There are no comments for this domain--</h3>\n";
                }
                else
                {
                    reportHtml += "<h2 style=\"text-align:center\">Comments</h2>\n" +
                                    "<table class=\"table\" style=\"width:700px;margin:auto;\">\n";
                    foreach (var comment in CommentList)
                    {
                        reportHtml += "<tr>\n" +
                                          $"<td> {comment.UpdatedBy} </td>\n" +
                                          $"<td> {comment.Comment} </td>\n" +
                                          $"<td> {comment.TimeOfComment} </td>\n" +
                                      "</tr>\n";
                                      
                    }
                    reportHtml += "</table>";
                }
            }

            StringReader reader = new StringReader(reportHtml);
            Document PdfFile = new Document(PageSize.A4);
            PdfWriter writer = PdfWriter.GetInstance(PdfFile, stream);
            PdfFile.Open();
            XMLWorkerHelper.GetInstance().ParseXHtml(writer, PdfFile, reader);
            PdfFile.Close();
            var instance = repository_SurveyInstance.SurveyAnswers.FirstOrDefault(q => instanceId == q.SurveyInstanceId);
            var SurveyName = (from sa in repository_SurveyInstance.SurveyAnswers
                              join q in repository_Question.Questions
                              on sa.QuestionId equals q.QuestionId
                              join t in repository_Topic.Topics
                              on q.TopicId equals t.TopicId
                              join d in repository_Domain.Domains
                              on t.DomainId equals d.DomainId
                              join s in repository_Survey.Surveys
                              on d.SurveyId equals s.SurveyId
                              where sa.inProgress == 0 && sa.SurveyInstanceId == instanceId
                              orderby d.DomainOrder, t.TopicOrder, q.QuestionOrder

                              select new QuestionInfo
                              {
                                  SurveyName = s.SurveyName   
                                  
                              }).ToList().First().SurveyName;

            return File(stream.ToArray(), "application/pdf", $"{repository_Program.Programs.FirstOrDefault(p => p.ProgramId == instance.ProgramId).ProgramName} - {SurveyName} {instance.SubmissionDate}.pdf");
        }

        public ViewResult Report(int instanceId)
        {
            var AnswerList = (from sa in repository_SurveyInstance.SurveyAnswers
                              join q in repository_Question.Questions
                              on sa.QuestionId equals q.QuestionId
                              join t in repository_Topic.Topics
                              on q.TopicId equals t.TopicId
                              join d in repository_Domain.Domains
                              on t.DomainId equals d.DomainId
                              join s in repository_Survey.Surveys
                              on d.SurveyId equals s.SurveyId
                              where sa.inProgress == 0 && sa.SurveyInstanceId == instanceId
                              orderby d.DomainOrder, t.TopicOrder, q.QuestionOrder

                              select new QuestionInfo
                              {
                                  QuestionId = q.QuestionId,
                                  QuestionOrder = q.QuestionOrder,
                                  SurveyAnswerId = sa.SurveyAnswerId,
                                  TopicId = t.TopicId,
                                  TopicOrder = t.TopicOrder,
                                  TopicName = t.TopicName,
                                  DomainId = d.DomainId,
                                  DomainOrder = d.DomainOrder,
                                  DomainName = d.DomainName,
                                  QuestionPrompt = q.QuestionPrompt,
                                  SurveyId = s.SurveyId,
                                  SurveyName = s.SurveyName,
                                  ProgramId = sa.ProgramId,
                                  SurveyInstanceId = sa.SurveyInstanceId,
                                  Answer = sa.Answer,
                                  SubmissionDate = sa.SubmissionDate

                              }).ToList();

            var DomainList = AnswerList.Distinct(new QuestionInfoDomainComparer()).ToList();
            var TopicList = AnswerList.Distinct(new QuestionInfoTopicComparer()).ToList();
            var QuestionList = AnswerList;
            var CommentList = repository_DomainComment.DomainComments.Where(p => p.SurveyInstanceId == instanceId).ToList();

            var instance = repository_SurveyInstance.SurveyAnswers.FirstOrDefault(q => instanceId == q.SurveyInstanceId);
            ViewBag.ProgramName = repository_Program.Programs.FirstOrDefault(p => p.ProgramId == instance.ProgramId).ProgramName;
            DomainHistoryViewModel viewModel = new DomainHistoryViewModel(AnswerList, DomainList, TopicList, QuestionList, CommentList, "-1,0,1,2,3,4,5");

            return View(viewModel);
        }

        public ViewResult EditHistory(int instanceId)
        {
            var InstanceAnswers = (from sa in repository_SurveyInstance.SurveyAnswers
                            join q in repository_Question.Questions
                            on sa.QuestionId equals q.QuestionId
                            join t in repository_Topic.Topics
                            on q.TopicId equals t.TopicId
                            join d in repository_Domain.Domains
                            on t.DomainId equals d.DomainId
                            join s in repository_Survey.Surveys
                            on d.SurveyId equals s.SurveyId
                            where sa.inProgress == 1 && sa.SurveyInstanceId == instanceId
                            orderby d.DomainOrder, t.TopicOrder, q.QuestionOrder

                            select new QuestionInfo
                            {
                                QuestionId = q.QuestionId,
                                QuestionOrder = q.QuestionOrder,
                                SurveyAnswerId = sa.SurveyAnswerId,
                                TopicId = t.TopicId,
                                TopicOrder = t.TopicOrder,
                                TopicName = t.TopicName,
                                DomainId = d.DomainId,
                                DomainOrder = d.DomainOrder,
                                DomainName = d.DomainName,
                                QuestionPrompt = q.QuestionPrompt,
                                SurveyId = s.SurveyId,
                                SurveyName = s.SurveyName,
                                ProgramId = sa.ProgramId,
                                SurveyInstanceId = sa.SurveyInstanceId

                            }).ToList();

            EditInstanceViewModel Model = new EditInstanceViewModel(null, null, null, null, null, 0, null);
            Model.Domains = InstanceAnswers.Distinct(new QuestionInfoDomainComparer()).ToList();
            Model.Topics = InstanceAnswers.Distinct(new QuestionInfoTopicComparer()).ToList();
            Model.Questions = InstanceAnswers;
            var instance = (from p in repository_SurveyInstance.SurveyAnswers
                            join e in repository_EditInstance.EditingInstances
                            on p.SurveyAnswerId equals e.SurveyAnswerId
                            where p.SurveyInstanceId == instanceId
                            select new Edit
                            {
                                QuestionId = p.QuestionId,
                                SurveyAnswerId = p.SurveyAnswerId,
                                EditInstanceId = e.EditInstanceId,
                                Answer = e.Answer,
                                AnswerText = e.AnswerText,
                                UserName = e.UserName,
                                UpdatedBy = e.UpdatedBy,
                                TimeOfEdit = e.TimeOfEdit                                
                            }).ToList();

            Model.Instances = instance;

            return View(Model);
        }
        public IActionResult DeleteHistory(int surveyInstanceId)
        {
            var surveyName = (from surveyInstance in repository_SurveyInstance.SurveyAnswers
                             join question in repository_Question.Questions
                                 on surveyInstance.QuestionId equals question.QuestionId
                             join topic in repository_Topic.Topics
                                 on question.TopicId equals topic.TopicId
                             join domain in repository_Domain.Domains
                                 on topic.DomainId equals domain.DomainId
                             join survey in repository_Survey.Surveys
                                 on domain.SurveyId equals survey.SurveyId
                             where surveyInstance.SurveyInstanceId == surveyInstanceId
                             select new QuestionInfo 
                             {
                                 SurveyName = survey.SurveyName,
                             }).ToList();

            List<SurveyInstance> deletedHistory = repository_SurveyInstance.DeleteSurvey(surveyInstanceId);
            

            if (deletedHistory.Count() != 0)
            {
                TempData["message"] = $"{$"The {surveyName.First().SurveyName} submitted on {deletedHistory[0].SubmissionDate} was deleted"}";
            }
            else
            {
                TempData["message"] = $"{surveyName.First().SurveyName} was bugged";
            }

            return RedirectToAction("SurveyHistory");
        }

        [HttpPost]
        public IActionResult SubmitComment(string Comment, int progId, int surveyId, int domId)
        {
            var userId = _userManager.GetUserId(HttpContext.User);
            var UserProgramList = repository_ProgramUser.ProgramUsers.Where(q => q.UserId == userId).ToList();
            var comments = new List<CommentInstance>();
            var instance = (from sa in repository_SurveyInstance.SurveyAnswers
                            join q in repository_Question.Questions
                            on sa.QuestionId equals q.QuestionId
                            join t in repository_Topic.Topics
                            on q.TopicId equals t.TopicId
                            join d in repository_Domain.Domains
                            on t.DomainId equals d.DomainId
                            join s in repository_Survey.Surveys
                            on d.SurveyId equals s.SurveyId
                            where sa.inProgress == 1 && d.SurveyId == surveyId
                            orderby d.DomainOrder, t.TopicOrder, q.QuestionOrder

                            select new QuestionInfo
                            {
                                QuestionId = q.QuestionId,
                                QuestionOrder = q.QuestionOrder,
                                SurveyAnswerId = sa.SurveyAnswerId,
                                TopicId = t.TopicId,
                                TopicOrder = t.TopicOrder,
                                TopicName = t.TopicName,
                                DomainId = d.DomainId,
                                DomainOrder = d.DomainOrder,
                                DomainName = d.DomainName,
                                QuestionPrompt = q.QuestionPrompt,
                                SurveyId = s.SurveyId,
                                SurveyName = s.SurveyName,
                                ProgramId = sa.ProgramId,
                                SurveyInstanceId = sa.SurveyInstanceId,
                                Answer = sa.Answer,

                            }).ToList();

            int instSurveyId = (from sa in repository_SurveyInstance.SurveyAnswers
                        join q in repository_Question.Questions
                        on sa.QuestionId equals q.QuestionId
                        join t in repository_Topic.Topics
                        on q.TopicId equals t.TopicId
                        join d in repository_Domain.Domains
                        on t.DomainId equals d.DomainId
                        join s in repository_Survey.Surveys
                        on d.SurveyId equals s.SurveyId
                        where progId == sa.ProgramId && s.SurveyId == surveyId && d.DomainId == domId && sa.inProgress == 1
                          
                        select new
                        {                             
                            SurveyInstanceId = sa.SurveyInstanceId
                        }).First().SurveyInstanceId;
         
            if (ModelState.IsValid)
            {
                DomainComment domComment = new DomainComment
                {
                    SurveyInstanceId = instSurveyId,
                    DomainId = domId,
                    UserId = userId,
                    UpdatedBy = UserProgramList.First().FirstName + ' ' + UserProgramList.First().LastName,
                    TimeOfComment = DateTime.Now,
                    Comment = Comment
                };
                if (Comment == null)
                {
                    ViewBag.error = true;
                    ViewBag.ProgId = progId;
                    if (domId != 0)
                    {
                        currentDomainId = domId;
                        ViewModel.Domain = repository_Domain.Domains.First(p => p.DomainId == domId);
                    }
                    else
                    {
                        ViewModel.Domain = repository_Domain.Domains.First(p => p.DomainId == currentDomainId);
                    }
                    var qList = repository_SurveyInstance.SurveyAnswers.Where(p => p.ProgramId == progId && p.inProgress == 1).ToList();
                    
                    comments = (from c in repository_DomainComment.DomainComments
                                join sa in repository_SurveyInstance.SurveyAnswers
                                on c.SurveyInstanceId equals sa.SurveyInstanceId
                                join d in repository_Domain.Domains
                                on c.DomainId equals d.DomainId
                                join s in repository_Survey.Surveys
                                on d.SurveyId equals s.SurveyId
                                where progId == sa.ProgramId && s.SurveyId == surveyId && c.DomainId == domId && sa.inProgress == 1
                                orderby d.DomainOrder

                                select new CommentInstance
                                {
                                    Comment = c.Comment,
                                    DomainCommentId = c.DomainCommentId,
                                    ProgramId = sa.ProgramId,
                                    DomainId = c.DomainId,
                                    SurveyId = s.SurveyId,
                                    SurveyInstanceId = sa.SurveyInstanceId,
                                    TimeOfComment = c.TimeOfComment,
                                    UpdatedBy = c.UpdatedBy,

                                }).Distinct(new CommentInstanceCommentComparer()).ToList();

                    ViewModel.Domains = instance.Distinct(new QuestionInfoDomainComparer()).ToList();
                    ViewModel.Topics = instance.Distinct(new QuestionInfoTopicComparer()).ToList();
                    ViewModel.Questions = instance;
                    ViewModel.DomainComments = comments;
                    ViewModel.SurveyAnswers = instance;

                    return View("CompleteSurvey", ViewModel);
                }

                if (repository_DomainComment.DomainComments.Count() == 0)
                {
                    domComment.DomainCommentId = 1;
                }
                else
                {
                    domComment.DomainCommentId = repository_DomainComment.DomainComments.Max(p => p.DomainCommentId) + 1;
                }

                repository_DomainComment.SaveDomainComment(domComment);
                comments = (from c in repository_DomainComment.DomainComments
                            join sa in repository_SurveyInstance.SurveyAnswers
                            on c.SurveyInstanceId equals sa.SurveyInstanceId
                            join d in repository_Domain.Domains
                            on c.DomainId equals d.DomainId
                            join s in repository_Survey.Surveys
                            on d.SurveyId equals s.SurveyId
                            where progId == sa.ProgramId && s.SurveyId == surveyId && c.DomainId == domId && sa.inProgress == 1
                            orderby d.DomainOrder

                            select new CommentInstance
                            {
                                Comment = c.Comment,
                                DomainCommentId = c.DomainCommentId,
                                ProgramId = sa.ProgramId,
                                DomainId = c.DomainId,
                                SurveyId = s.SurveyId,
                                SurveyInstanceId = sa.SurveyInstanceId,
                                TimeOfComment = c.TimeOfComment,
                                UpdatedBy = c.UpdatedBy,

                            }).Distinct(new CommentInstanceCommentComparer()).ToList();
            }

            ViewBag.ProgId = progId;
            if (domId != 0)
            {
                currentDomainId = domId;
                ViewModel.Domain = repository_Domain.Domains.First(p => p.DomainId == domId);
            }
            else
            {
                ViewModel.Domain = repository_Domain.Domains.First(p => p.DomainId == currentDomainId);
            }
            var questionList =  (from sa in repository_SurveyInstance.SurveyAnswers
                                join q in repository_Question.Questions
                                on sa.QuestionId equals q.QuestionId
                                join t in repository_Topic.Topics
                                on q.TopicId equals t.TopicId
                                join d in repository_Domain.Domains
                                on t.DomainId equals d.DomainId
                                join s in repository_Survey.Surveys
                                on d.SurveyId equals s.SurveyId
                                where sa.inProgress == 1 && d.SurveyId == surveyId
                                orderby d.DomainOrder, t.TopicOrder, q.QuestionOrder

                                select new QuestionInfo
                                {
                                    QuestionId = q.QuestionId,
                                    QuestionOrder = q.QuestionOrder,
                                    SurveyAnswerId = sa.SurveyAnswerId,
                                    TopicId = t.TopicId,
                                    TopicOrder = t.TopicOrder,
                                    TopicName = t.TopicName,
                                    DomainId = d.DomainId,
                                    DomainOrder = d.DomainOrder,
                                    DomainName = d.DomainName,
                                    QuestionPrompt = q.QuestionPrompt,
                                    SurveyId = s.SurveyId,
                                    SurveyName = s.SurveyName,
                                    ProgramId = sa.ProgramId

                                }).ToList();

            ViewModel.SurveyAnswers = instance;
            ViewModel.Domains = questionList.Distinct(new QuestionInfoDomainComparer()).ToList();
            ViewModel.Topics = questionList.Distinct(new QuestionInfoTopicComparer()).ToList();
            ViewModel.Questions = questionList;
            ViewModel.DomainComments = comments;

            return View("CompleteSurvey", ViewModel);
        }

        [HttpPost]
        public ViewResult DeleteComment(int domcomId, int progId, int surveyId, int domId)
        {
            var comment = repository_DomainComment.DeleteDomainComment(domcomId);
            if (comment != null)
            {
                TempData["message"] = $"{comment.UpdatedBy}'s comment was deleted";
            }
            ViewBag.ProgId = progId;
            if (domId != 0)
            {
                currentDomainId = domId;
                ViewModel.Domain = repository_Domain.Domains.First(p => p.DomainId == domId);
            }
            else
            {
                ViewModel.Domain = repository_Domain.Domains.First(p => p.DomainId == currentDomainId);
            }
            var instance = (from sa in repository_SurveyInstance.SurveyAnswers
                            join q in repository_Question.Questions
                            on sa.QuestionId equals q.QuestionId
                            join t in repository_Topic.Topics
                            on q.TopicId equals t.TopicId
                            join d in repository_Domain.Domains
                            on t.DomainId equals d.DomainId
                            join s in repository_Survey.Surveys
                            on d.SurveyId equals s.SurveyId
                            where sa.inProgress == 1 && d.SurveyId == surveyId
                            orderby d.DomainOrder, t.TopicOrder, q.QuestionOrder

                            select new QuestionInfo
                            {
                                QuestionId = q.QuestionId,
                                QuestionOrder = q.QuestionOrder,
                                SurveyAnswerId = sa.SurveyAnswerId,
                                TopicId = t.TopicId,
                                TopicOrder = t.TopicOrder,
                                TopicName = t.TopicName,
                                DomainId = d.DomainId,
                                DomainOrder = d.DomainOrder,
                                DomainName = d.DomainName,
                                QuestionPrompt = q.QuestionPrompt,
                                SurveyId = s.SurveyId,
                                SurveyName = s.SurveyName,
                                ProgramId = sa.ProgramId,
                                SurveyInstanceId = sa.SurveyInstanceId,
                                Answer = sa.Answer,

                            }).ToList();

            var questionList = (from sa in repository_SurveyInstance.SurveyAnswers
                                join q in repository_Question.Questions
                                on sa.QuestionId equals q.QuestionId
                                join t in repository_Topic.Topics
                                on q.TopicId equals t.TopicId
                                join d in repository_Domain.Domains
                                on t.DomainId equals d.DomainId
                                join s in repository_Survey.Surveys
                                on d.SurveyId equals s.SurveyId
                                where sa.inProgress == 1 && d.SurveyId == surveyId
                                orderby d.DomainOrder, t.TopicOrder, q.QuestionOrder

                                select new QuestionInfo
                                {
                                    QuestionId = q.QuestionId,
                                    QuestionOrder = q.QuestionOrder,
                                    SurveyAnswerId = sa.SurveyAnswerId,
                                    TopicId = t.TopicId,
                                    TopicOrder = t.TopicOrder,
                                    TopicName = t.TopicName,
                                    DomainId = d.DomainId,
                                    DomainOrder = d.DomainOrder,
                                    DomainName = d.DomainName,
                                    QuestionPrompt = q.QuestionPrompt,
                                    SurveyId = s.SurveyId,
                                    SurveyName = s.SurveyName,
                                    ProgramId = sa.ProgramId

                                }).ToList();

            var comments = (from c in repository_DomainComment.DomainComments
                            join sa in repository_SurveyInstance.SurveyAnswers
                            on c.SurveyInstanceId equals sa.SurveyInstanceId
                            join d in repository_Domain.Domains
                            on c.DomainId equals d.DomainId
                            join s in repository_Survey.Surveys
                            on d.SurveyId equals s.SurveyId
                            where progId == sa.ProgramId && s.SurveyId == surveyId && c.DomainId == domId && sa.inProgress == 1
                            orderby d.DomainOrder

                            select new CommentInstance
                            {
                                Comment = c.Comment,
                                DomainCommentId = c.DomainCommentId,
                                ProgramId = sa.ProgramId,
                                DomainId = c.DomainId,
                                SurveyId = s.SurveyId,
                                SurveyInstanceId = sa.SurveyInstanceId,
                                TimeOfComment = c.TimeOfComment,
                                UpdatedBy = c.UpdatedBy,

                            }).Distinct(new CommentInstanceCommentComparer()).ToList();

            ViewModel.SurveyAnswers = instance;
            ViewModel.Domains = questionList.Distinct(new QuestionInfoDomainComparer()).ToList();
            ViewModel.Topics = questionList.Distinct(new QuestionInfoTopicComparer()).ToList();
            ViewModel.Questions = questionList;
            ViewModel.DomainComments = comments;

            return View("CompleteSurvey", ViewModel);
        }
    }
}
