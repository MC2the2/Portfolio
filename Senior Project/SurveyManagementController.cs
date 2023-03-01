using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.AspNetCore.Authorization;
using FANQAPortal.Models;
using Microsoft.EntityFrameworkCore;
using System.IO;
using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.tool.xml;
using System.Linq.Expressions;

namespace FANQAPortal.Controllers
{
    //[Authorize(Roles = "SuperAdmin")]
    public class SurveyManagementController : Controller
    {
        private IQuestionRepository questionRepo;
        private IDomainRepository domainRepo;
        private ITopicRepository topicRepo;
        private TopicDomainViewModel ViewModel;
        private static int CurrentDomainId;
        private static int Code;
        private Domain[] domainArray;
        private Topic[] topicArray;
        private Question[] questionArray;
        private ISurveyRepository surveyRepo;

        public SurveyManagementController(IQuestionRepository repo, IDomainRepository drepo, ITopicRepository trepo, ISurveyRepository srepo)
        {
            questionRepo = repo;
            domainRepo = drepo;
            topicRepo = trepo;
            surveyRepo = srepo;
            ViewModel = new TopicDomainViewModel(null, null, null, null, null);
        }

        public ViewResult AddDomain(int SurveyNo)
        {
            Domain domain = new Domain
            {
                DomainId = 0,
                DomainOrder = domainRepo.Domains.Count(p => p.SurveyId == SurveyNo) + 1,
                Active = 1,
                SurveyId = SurveyNo
            };
            int Code = 3;
            return View("EditDomain", new DomainViewModel(domain, Code, domain.DomainOrder, surveyRepo.Surveys.FirstOrDefault(p => p.SurveyId == SurveyNo).SurveyName));
        }

        public ViewResult AddQ(int TopicNo)
        {
            Question question = new Question
            {
                QuestionId = 0,
                TopicId = TopicNo,
                Active = 1,
                QuestionOrder = questionRepo.Questions.Count(p => p.TopicId == TopicNo) + 1
            };
            Code = 11;
            return View("EditQuestion", new QuestionViewModel(question, Code, question.QuestionOrder, topicRepo.Topics.FirstOrDefault(p => p.TopicId == TopicNo).TopicName));
        }
        public ViewResult AddSurvey()
        {
            Survey survey = new Survey
            {
                SurveyId = 0,
                Open = 0,
            };
            int Code = 15;
            return View("EditSurvey", new SurveyViewModel(survey, Code));
        }
        public ViewResult AddTopic(int domainNo)
        {
            Topic topic = new Topic
            {
                DomainId = domainNo,
                TopicId = 0,
                Active = 1,
                TopicOrder = topicRepo.Topics.Count(p => p.DomainId == CurrentDomainId) + 1
            };
            Code = 7;
            return View("EditTopic", new TopicViewModel(topic, Code, topic.TopicOrder, domainRepo.Domains.FirstOrDefault(p => p.DomainId == domainNo).DomainName));
        }

        public ViewResult EditQuestion(int questionNo)
        {
            Question question = questionRepo.Questions.FirstOrDefault(p => p.QuestionId == questionNo);
            Code = 12;
            return View(new QuestionViewModel(question, Code, questionRepo.Questions.Count(p => p.TopicId == question.TopicId), topicRepo.Topics.FirstOrDefault(p => p.TopicId == question.TopicId).TopicName));
        }

        [HttpPost]
        public IActionResult EditQuestion(Question question)
        {
            int numberToCheck = question.QuestionId == 0 ? questionRepo.Questions.Count(p => p.TopicId == question.TopicId) + 1 : questionRepo.Questions.Count(p => p.TopicId == question.TopicId);
            if (ModelState.IsValid)
            {
                int index = question.QuestionOrder;
                if (questionRepo.Questions.Count(p => p.TopicId == question.TopicId) == 0)
                {
                    questionRepo.SaveQuestion(question);
                }
                else
                {
                    int max = questionRepo.Questions.Where(p => p.TopicId == question.TopicId).Max(p => p.QuestionOrder);
                    int oldNumber = question.QuestionId == 0 ? questionRepo.Questions.Count(p => p.TopicId == question.TopicId) + 1 : questionRepo.Questions.First(d => d.QuestionId == question.QuestionId).QuestionOrder;
                    if (max > numberToCheck && (questionRepo.Questions.Count(p => p.QuestionOrder == question.QuestionOrder && p.TopicId == question.TopicId) == 1))
                    {
                        bool noLower = false;
                        bool noHigher = false;

                        while (questionRepo.Questions.Count(p => p.QuestionOrder == index && p.TopicId == question.TopicId) >= 1 && index > 0 && index != oldNumber)
                        {
                            index--;
                        }
                        if (index == 0)
                        {
                            noLower = true;
                        }
                        int lowerbound = index;
                        index = question.QuestionOrder;

                        while (questionRepo.Questions.Count(p => p.QuestionOrder == index && p.TopicId == question.TopicId) >= 1 && index <= max && index != oldNumber)
                        {
                            index++;
                        }
                        if (index == max + 1)
                        {
                            noHigher = true;
                        }
                        int higherbound = index;
                        if (noLower == true || higherbound - question.QuestionOrder <= question.QuestionOrder - lowerbound)
                        {
                            index = 0;
                            questionArray = new Question[questionRepo.Questions.Count(p => p.QuestionOrder >= question.QuestionOrder && p.QuestionOrder < higherbound && p.TopicId == question.TopicId)];
                            foreach (Question questiona in questionRepo.Questions.Where(p => p.QuestionOrder >= question.QuestionOrder && p.QuestionOrder < higherbound && p.TopicId == question.TopicId))
                            {
                                questiona.QuestionOrder++;
                                questionArray[index] = questiona;
                                index++;
                            }
                        }
                        else if (noHigher == true || higherbound - question.QuestionOrder > question.QuestionOrder - lowerbound)
                        {
                            index = 0;
                            questionArray = new Question[questionRepo.Questions.Count(p => p.QuestionOrder <= question.QuestionOrder && p.QuestionOrder > lowerbound && p.TopicId == question.TopicId)];
                            foreach (Question questiona in questionRepo.Questions.Where(p => p.QuestionOrder <= question.QuestionOrder && p.QuestionOrder > lowerbound && p.TopicId == question.TopicId))
                            {
                                questiona.QuestionOrder--;
                                questionArray[index] = questiona;
                                index++;
                            }
                        }
                        index = 0;
                        while (index < questionArray.Length)
                        {
                            questionRepo.SaveQuestion(questionArray[index]);
                            index++;
                        }
                    }
                    else if (questionRepo.Questions.Count(p => (p.QuestionOrder == question.QuestionOrder) && (p.TopicId == question.TopicId)) == 1)
                    {
                        index = 0;
                        if (question.QuestionOrder < oldNumber)
                        {
                            questionArray = new Question[questionRepo.Questions.Count(p => p.QuestionOrder >= question.QuestionOrder && p.QuestionOrder < oldNumber && p.TopicId == question.TopicId)];
                            foreach (Question queso in questionRepo.Questions.Where(p => p.QuestionOrder >= question.QuestionOrder && p.QuestionOrder < oldNumber && p.TopicId == question.TopicId))
                            {
                                queso.QuestionOrder++;
                                questionArray[index] = queso;
                                index++;
                            }
                        }
                        else
                        {
                            questionArray = new Question[questionRepo.Questions.Count(p => p.QuestionOrder <= question.QuestionOrder && p.QuestionOrder > oldNumber && p.TopicId == question.TopicId)];
                            foreach (Question queso in questionRepo.Questions.Where(p => p.QuestionOrder <= question.QuestionOrder && p.QuestionOrder > oldNumber && p.TopicId == question.TopicId))
                            {
                                queso.QuestionOrder--;
                                questionArray[index] = queso;
                                index++;
                            }
                        }
                        index = 0;
                        while (index < questionArray.Length)
                        {
                            questionRepo.SaveQuestion(questionArray[index]);
                            index++;
                        }
                    }

                    questionRepo.SaveQuestion(question);
                }
                TempData["message"] = "Your question has been saved";
                return RedirectToAction("ViewTopics");
            }
            else
            {
                //there is something wrong with the data values
                return View(new QuestionViewModel(question, Code, numberToCheck, topicRepo.Topics.FirstOrDefault(p => p.TopicId == question.TopicId).TopicName));
            }
        }

        public IActionResult ShowHideQuestion(int QuestionNo, string topicSortOrder, string questionSortOrder)
        {
            Question question = questionRepo.Questions.FirstOrDefault(p => p.QuestionId == QuestionNo);
            if (question.Active == 1)
            {
                question.Active = 0;
                TempData["Message"] = "The question has now been hidden from the survey.";
            }
            else
            {
                question.Active = 1;
                TempData["Message"] = "The question will now show on the survey.";
            }
            questionRepo.SaveQuestion(question);
            return RedirectToAction("ViewTopics", new { topicSortOrder = topicSortOrder, questionSortOrder = questionSortOrder });
        }

        [HttpPost]
        public IActionResult DeleteQuestion(int QuestionNo, string topicSortOrder, string questionSortOrder)
        {

            Question question = questionRepo.DeleteQuestion(QuestionNo);
            if (question != null)
            {
                TempData["message"] = $"Question {question.QuestionOrder} was deleted";
            }
           
            return RedirectToAction("ViewTopics", new { topicSortOrder = topicSortOrder, questionSortOrder = questionSortOrder });
        }

        public ViewResult EditDomain(int domainNo)
        {
            Domain domain = domainRepo.Domains.FirstOrDefault(p => p.DomainId == domainNo);
            Code = 4;
            return View(new DomainViewModel(domain, Code, domainRepo.Domains.Count(p => p.SurveyId == domain.SurveyId), surveyRepo.Surveys.FirstOrDefault(p => p.SurveyId == domain.SurveyId).SurveyName));
        }

        [HttpPost]
        public IActionResult EditDomain(Domain domain)
        {
            int numberToCheck = domain.DomainId == 0 ? domainRepo.Domains.Count(p => p.SurveyId == domain.SurveyId) + 1 : domainRepo.Domains.Count(p => p.SurveyId == domain.SurveyId);

            if (ModelState.IsValid)
            {
                int index = domain.DomainOrder;
                if (domainRepo.Domains.Count(p => p.SurveyId == domain.SurveyId) == 0)
                {
                    domainRepo.SaveDomain(domain);
                }
                else
                {
                    int max = domainRepo.Domains.Where(p => p.SurveyId == domain.SurveyId).Max(p => p.DomainOrder);
                    int oldNumber = domain.DomainId == 0 ? domainRepo.Domains.Count() + 1 : domainRepo.Domains.First(d => d.DomainId == domain.DomainId).DomainOrder;
                    if (max > numberToCheck && (domainRepo.Domains.Count(p => p.DomainOrder == domain.DomainOrder && p.SurveyId == p.DomainId) == 1))
                    {
                        bool noLower = false;
                        bool noHigher = false;

                        while (domainRepo.Domains.Count(p => p.DomainOrder == index && p.SurveyId == domain.SurveyId) >= 1 && index > 0 && index != oldNumber)
                        {
                            index--;
                        }
                        if (index == 0)
                        {
                            noLower = true;
                        }
                        int lowerbound = index;
                        index = domain.DomainOrder;

                        while (domainRepo.Domains.Count(p => p.DomainOrder == index && p.SurveyId == domain.SurveyId) >= 1 && index <= max && index != oldNumber)
                        {
                            index++;
                        }
                        if (index == max + 1)
                        {
                            noHigher = true;
                        }
                        int higherbound = index;
                        if (noLower == true || higherbound - domain.DomainOrder <= domain.DomainOrder - lowerbound)
                        {
                            index = 0;
                            domainArray = new Domain[domainRepo.Domains.Count(p => p.DomainOrder >= domain.DomainOrder && p.DomainOrder < higherbound)];
                            foreach (Domain domane in domainRepo.Domains.Where(p => p.DomainOrder >= domain.DomainOrder && p.DomainOrder < higherbound))
                            {
                                domane.DomainOrder++;
                                domainArray[index] = domane;
                                index++;
                            }
                        }
                        else if (noHigher == true || higherbound - domain.DomainOrder > domain.DomainOrder - lowerbound)
                        {
                            index = 0;
                            domainArray = new Domain[domainRepo.Domains.Count(p => p.DomainOrder <= domain.DomainOrder && p.DomainOrder > lowerbound)];
                            foreach (Domain domane in domainRepo.Domains.Where(p => p.DomainOrder <= domain.DomainOrder && p.DomainOrder > lowerbound))
                            {
                                domane.DomainOrder--;
                                domainArray[index] = domane;
                                index++;
                            }
                            index = 0;
                        }
                        while (index < domainArray.Length)
                        {
                            domainRepo.SaveDomain(domainArray[index]);
                            index++;
                        }
                    }
                    else if (domainRepo.Domains.Count(p => p.DomainOrder == domain.DomainOrder && p.SurveyId == domain.SurveyId) == 1)
                    {

                        index = 0;
                        if (domain.DomainOrder < oldNumber)
                        {
                            domainArray = new Domain[domainRepo.Domains.Count(p => p.DomainOrder >= domain.DomainOrder && p.DomainOrder < oldNumber && p.SurveyId == domain.SurveyId)];
                            foreach (Domain domane in domainRepo.Domains.Where(p => p.DomainOrder >= domain.DomainOrder && p.DomainOrder < oldNumber && p.SurveyId == domain.SurveyId))
                            {
                                domane.DomainOrder++;
                                domainArray[index] = domane;
                                index++;
                            }
                        }
                        else
                        {
                            domainArray = new Domain[domainRepo.Domains.Count(p => p.DomainOrder <= domain.DomainOrder && p.DomainOrder > oldNumber && p.SurveyId == domain.SurveyId)];
                            foreach (Domain domane in domainRepo.Domains.Where(p => p.DomainOrder <= domain.DomainOrder && p.DomainOrder > oldNumber && p.SurveyId == domain.SurveyId))
                            {
                                domane.DomainOrder--;
                                domainArray[index] = domane;
                                index++;
                            }
                        }
                        index = 0;
                        while (index < domainArray.Length)
                        {

                            domainRepo.SaveDomain(domainArray[index]);
                            index++;
                        }
                    }
                    domainRepo.SaveDomain(domain);
                }
                TempData["Message"] = "Your domain has been saved.";
                return RedirectToAction("ViewSurveys");
            }

            else
            {
                return View(new DomainViewModel(domain, Code, numberToCheck, surveyRepo.Surveys.FirstOrDefault(p => p.SurveyId == domain.SurveyId).SurveyName));
            }
        }

        public IActionResult ShowHideDomain(int DomainNo, string domainSortOrder, string surveyOrder)
        {
            Domain domain = domainRepo.Domains.FirstOrDefault(p => p.DomainId == DomainNo);
            if (domain.Active == 1)
            {
                domain.Active = 0;
                TempData["Message"] = "The domain has now been hidden from the survey.";
            }
            else
            {
                domain.Active = 1;
                TempData["Message"] = "The domain will now show on the survey.";
            }
            domainRepo.SaveDomain(domain);

            return RedirectToAction("ViewSurveys", new { domainSortOrder = domainSortOrder, surveyOrder = surveyOrder, surveyNo = domain.SurveyId });
        }

        [HttpPost]
        public IActionResult DeleteDomain(int DomainNo, string domainSortOrder, string surveyOrder)
        {

            Domain domain = domainRepo.DeleteDomain(DomainNo);
            if (domain != null)
            {
                TempData["message"] = $"Domain {domain.DomainOrder} was deleted";
            }

            return RedirectToAction("ViewSurveys", new { domainSortOrder = domainSortOrder, surveyOrder = surveyOrder });
        }
        public ViewResult ViewSurveys(string domainSortOrder, string surveySortOrder, int surveyNo)
        {
            ViewBag.SurveyNo = 1;
            ViewData["DNameSortParm"] = domainSortOrder == "name_asc" ? "name_desc" : "name_asc";
            ViewData["DPreamble"] = domainSortOrder == "Asc" ? "Dsc" : "Asc";
            ViewData["DActive"] = domainSortOrder == "Active" ? "Inactive" : "Active";
            ViewData["DId"] = domainSortOrder == "IAsc" ? "IDsc" : "IAsc";
            ViewData["DOrder"] = domainSortOrder == "OAsc" ? "ODsc" : "OAsc";

            ViewData["SNameSortParm"] = surveySortOrder == "name_asc" ? "name_desc" : "name_asc";
            ViewData["SDescription"] = surveySortOrder == "Asc" ? "Dsc" : "Asc";
            ViewData["SActive"] = surveySortOrder == "Active" ? "Inactive" : "Active";
            ViewData["SId"] = surveySortOrder == "IAsc" ? "IDsc" : "IAsc";

            var domains = domainRepo.Domains;
            var surveys = surveyRepo.Surveys;

            switch (surveySortOrder)
            {
                case "name_asc":
                    surveys = surveys.OrderBy(s => s.SurveyName);
                    break;
                case "name_desc":
                    surveys = surveys.OrderByDescending(s => s.SurveyName);
                    break;
                case "Asc":
                    surveys = surveys.OrderBy(s => s.SurveyDescription);
                    break;
                case "Dsc":
                    surveys = surveys.OrderByDescending(s => s.SurveyDescription);
                    break;
                case "IDsc":
                    surveys = surveys.OrderByDescending(s => s.SurveyId);
                    break;
                case "IAsc":
                    surveys = surveys.OrderBy(s => s.SurveyId);
                    break;
                case "Active":
                    surveys = surveys.OrderBy(s => s.Open);
                    break;
                case "Inactive":
                    surveys = surveys.OrderByDescending(s => s.Open);
                    break;
                default:
                    surveys = surveys.OrderBy(s => s.SurveyName);
                    break;
            }
            switch (domainSortOrder)
            {
                case "name_asc":
                    domains = domains.OrderBy(s => s.DomainName);
                    break;

                case "name_desc":
                    domains = domains.OrderByDescending(s => s.DomainName);
                    break;
                case "Asc":
                    domains = domains.OrderBy(s => s.DomainPreamble);
                    break;
                case "Dsc":
                    domains = domains.OrderByDescending(s => s.DomainPreamble);
                    break;
                case "IDsc":
                    domains = domains.OrderByDescending(s => s.DomainId);
                    break;
                case "IAsc":
                    domains = domains.OrderBy(s => s.DomainId);
                    break;
                case "Active":
                    domains = domains.OrderBy(s => s.Active);
                    break;
                case "Inactive":
                    domains = domains.OrderByDescending(s => s.Active);
                    break;
                case "OAsc":
                    domains = domains.OrderBy(s => s.DomainOrder);
                    break;
                case "ODsc":
                    domains = domains.OrderByDescending(s => s.DomainOrder);
                    break;
                default:
                    domains = domains.OrderBy(s => s.DomainOrder);
                    break;
            }
            ViewBag.domainSortOrder = domainSortOrder;
            ViewBag.surveySortOrder = surveySortOrder;
            var SurveyList = surveys.ToList();
            if (surveyNo != 0)
            {
                ViewBag.SurveyNo = SurveyList.IndexOf(SurveyList.FirstOrDefault(p => p.SurveyId == surveyNo));
            }
            else
            {
                ViewBag.SurveyNo = -1;
            }
            return View(new SurveyDomainViewModel(domains.ToList(), SurveyList));
        }

        public ViewResult EditSurvey(int surveyNo)
        {
            Survey survey = surveyRepo.Surveys.FirstOrDefault(p => p.SurveyId == surveyNo);
            Code = 16;
            return View(new SurveyViewModel(survey, Code));
        }

        [HttpPost]
        public IActionResult EditSurvey(Survey survey)
        {
            if (ModelState.IsValid)
            {
                surveyRepo.SaveSurvey(survey);
                TempData["Message"] = "Your survey has been saved.";
                return RedirectToAction("ViewSurveys");
            }
            else
            {
                return View(new SurveyViewModel(survey, Code));
            }
        }
        public IActionResult DuplicateSurvey(int SurveyNo, string surveySortOrder, string domainSortOrder)
        {
            var Domains = domainRepo.Domains.Where(p => p.SurveyId == SurveyNo).ToList();
            var Topics = topicRepo.Topics.Where(p => domainRepo.Domains.FirstOrDefault(q => q.SurveyId == SurveyNo && q.DomainId == p.DomainId) != default).ToList();
            var Questions = questionRepo.Questions.Where(p => topicRepo.Topics.FirstOrDefault(q => q.TopicId == p.TopicId) != default).ToList();

            Survey survey = surveyRepo.Surveys.FirstOrDefault(p => p.SurveyId == SurveyNo);
            survey.SurveyId = 0;
            surveyRepo.SaveSurvey(survey);
            
            var Max = surveyRepo.Surveys.ToList().Last().SurveyId;

            for (int i = 0; i < Domains.Count(); i++)
            {
                var OldDomainId = Domains.ElementAt(i).DomainId;
                Domains.ElementAt(i).DomainId = 0;
                Domains.ElementAt(i).SurveyId = Max;
                domainRepo.SaveDomain(Domains.ElementAt(i));
                var CurrentTopicList = Topics.Where(p => p.DomainId == OldDomainId).ToList();
                var NewDomainId = domainRepo.Domains.ToList().Last().DomainId;

                for(int j = 0; j < CurrentTopicList.Count(); j++)
                {
                    var OldTopicId = CurrentTopicList.ElementAt(j).TopicId;
                    Topics.ElementAt(j).TopicId = 0;
                    Topics.ElementAt(j).DomainId = NewDomainId;
                    topicRepo.SaveTopic(Topics.ElementAt(j));
                    var CurrentQuestionList = Questions.Where(p => p.TopicId == OldTopicId).ToList();
                    var NewTopicId = topicRepo.Topics.ToList().Last().TopicId;

                    for (int k = 0; k < CurrentQuestionList.Count(); k++)
                    {
                        var OldQuestionId = CurrentQuestionList.ElementAt(k).QuestionId;
                        Questions.ElementAt(k).QuestionId = 0;
                        Questions.ElementAt(k).TopicId = NewTopicId;
                        questionRepo.SaveQuestion(Questions.ElementAt(k));
                    }
                }
            }
            return RedirectToAction("ViewSurveys", new { surveySortOrder = surveySortOrder, domainSortOrder = domainSortOrder, surveyNo = SurveyNo });
        }
        public IActionResult OpenCloseSurvey(int SurveyNo, string surveySortOrder, string domainSortOrder)
        {
            Survey survey = surveyRepo.Surveys.FirstOrDefault(p => p.SurveyId == SurveyNo);
            if (survey.Open == 1)
            {
                survey.Open = 0;
                TempData["Message"] = "The survey has been closed.";
            }
            else
            {
                survey.Open = 1;
                TempData["Message"] = "The survey is now open.";
            }
            surveyRepo.SaveSurvey(survey);

            return RedirectToAction("ViewSurveys", new { surveySortOrder = surveySortOrder, domainSortOrder = domainSortOrder, surveyNo = SurveyNo });
        }

        [HttpPost]
        public IActionResult DeleteSurvey(int SurveyNo, string surveySortOrder, string domainSortOrder)
        {

            Survey survey = surveyRepo.DeleteSurvey(SurveyNo);
            if (survey != null)
            {
                TempData["message"] = $"{survey.SurveyName} was deleted";
            }

            return RedirectToAction("ViewSurveys", new { surveySortOrder = surveySortOrder, domainSortOrder = domainSortOrder, surveyNo = SurveyNo });
        }
        public ViewResult EditTopic(int topicNo)
        {
            Topic topic = topicRepo.Topics.FirstOrDefault(p => p.TopicId == topicNo);
            Code = 8;
            return View(new TopicViewModel(topic, Code, topicRepo.Topics.Count(p => p.DomainId == topic.DomainId), domainRepo.Domains.FirstOrDefault(p => p.DomainId == topic.DomainId).DomainName));
        }

        [HttpPost]
        public IActionResult EditTopic(Topic topic)
        {
            int numberToCheck = topic.TopicId == 0 ? topicRepo.Topics.Count(p => p.DomainId == topic.DomainId) + 1 : topicRepo.Topics.Count(p => p.DomainId == topic.DomainId);
            if (ModelState.IsValid)
            {
                int index = topic.TopicOrder;
                if (topicRepo.Topics.Count(p => p.DomainId == topic.DomainId) == 0)
                {
                    topicRepo.SaveTopic(topic);
                }
                else
                {
                    int max = topicRepo.Topics.Where(p => p.DomainId == topic.DomainId).Max(p => p.TopicOrder);

                    int oldNumber = topic.TopicId == 0 ? topicRepo.Topics.Count(p => p.DomainId == topic.DomainId) + 1 : topicRepo.Topics.First(d => d.TopicId == topic.TopicId).TopicOrder;
                    if (max > numberToCheck && (topicRepo.Topics.Count(p => p.TopicOrder == topic.TopicOrder && p.DomainId == topic.DomainId) == 1))
                    {

                        bool noLower = false;
                        bool noHigher = false;

                        while (topicRepo.Topics.Count(p => p.TopicOrder == index && p.DomainId == topic.DomainId) >= 1 && index > 0 && index != oldNumber)
                        {
                            index--;
                        }
                        if (index == 0)
                        {
                            noLower = true;
                        }
                        int lowerbound = index;
                        index = topic.TopicOrder;

                        while (topicRepo.Topics.Count(p => p.TopicOrder == index && p.DomainId == topic.DomainId) >= 1 && index <= max && index != oldNumber)
                        {
                            index++;
                        }
                        if (index == max + 1)
                        {
                            noHigher = true;
                        }
                        int higherbound = index;
                        if (noLower == true || higherbound - topic.TopicOrder <= topic.TopicOrder - lowerbound)
                        {
                            index = 0;
                            topicArray = new Topic[topicRepo.Topics.Count(p => p.TopicOrder >= topic.TopicOrder && p.TopicOrder < higherbound && p.DomainId == topic.DomainId)];
                            foreach (Topic topica in topicRepo.Topics.Where(p => p.TopicOrder >= topic.TopicOrder && p.TopicOrder < higherbound && p.DomainId == topic.DomainId))
                            {
                                topica.TopicOrder++;
                                topicArray[index] = topica;
                                index++;
                            }
                        }
                        else if (noHigher == true || higherbound - topic.TopicOrder > topic.TopicOrder - lowerbound)
                        {
                            index = 0;
                            topicArray = new Topic[topicRepo.Topics.Count(p => p.TopicOrder <= topic.TopicOrder && p.TopicOrder > lowerbound && p.DomainId == topic.DomainId)];
                            foreach (Topic topica in topicRepo.Topics.Where(p => p.TopicOrder <= topic.TopicOrder && p.TopicOrder > lowerbound && p.DomainId == topic.DomainId))
                            {
                                topica.TopicOrder--;
                                topicArray[index] = topica;
                                index++;
                            }
                        }
                        index = 0;
                        while (index < topicArray.Length)
                        {

                            topicRepo.SaveTopic(topicArray[index]);
                            index++;
                        }
                    }
                    else if (topicRepo.Topics.Count(p => (p.TopicOrder == topic.TopicOrder) && (p.DomainId == topic.DomainId)) == 1)
                    {

                        index = 0;
                        if (topic.TopicOrder < oldNumber)
                        {
                            topicArray = new Topic[topicRepo.Topics.Count(p => p.TopicOrder >= topic.TopicOrder && p.TopicOrder < oldNumber && p.DomainId == topic.DomainId)];
                            foreach (Topic top in topicRepo.Topics.Where(p => p.TopicOrder >= topic.TopicOrder && p.TopicOrder < oldNumber && p.DomainId == topic.DomainId))
                            {
                                top.TopicOrder++;
                                topicArray[index] = top;
                                index++;
                            }
                        }
                        else
                        {
                            topicArray = new Topic[topicRepo.Topics.Count(p => p.TopicOrder <= topic.TopicOrder && p.TopicOrder > oldNumber && p.DomainId == topic.DomainId)];
                            foreach (Topic top in topicRepo.Topics.Where(p => p.TopicOrder <= topic.TopicOrder && p.TopicOrder > oldNumber && p.DomainId == topic.DomainId))
                            {
                                top.TopicOrder--;
                                topicArray[index] = top;
                                index++;
                            }
                        }
                        index = 0;
                        while (index < topicArray.Length)
                        {

                            topicRepo.SaveTopic(topicArray[index]);
                            index++;
                        }
                    }

                    topicRepo.SaveTopic(topic);
                }
                TempData["Message"] = "Your topic has been saved.";
                return RedirectToAction("ViewTopics");
            }
            else
            {
                // there is something wrong with the data value

                return View(new TopicViewModel(topic, Code, numberToCheck, domainRepo.Domains.FirstOrDefault(p => p.DomainId == topic.DomainId).DomainName));

            }
        }

        public IActionResult ShowHideTopic(int TopicNo, string topicSortOrder, string questionSortOrder)
        {
            Topic topic = topicRepo.Topics.FirstOrDefault(p => p.TopicId == TopicNo);
            if (topic.Active == 1)
            {
                topic.Active = 0;
                TempData["Message"] = "The topic has now been hidden from the survey.";
            }
            else
            {
                topic.Active = 1;
                TempData["Message"] = "The topic will now show on the survey.";
            }
            topicRepo.SaveTopic(topic);
            return RedirectToAction("ViewTopics", new { topicSortOrder = topicSortOrder, questionSortOrder = questionSortOrder });
        }

        [HttpPost]
        public IActionResult DeleteTopic(int TopicNo, string topicSortOrder, string questionSortOrder)
        {

            Topic topic = topicRepo.DeleteTopic(TopicNo);
            if (topic != null)
            {
                TempData["message"] = $"Topic {topic.TopicOrder} was deleted";
            }

            return RedirectToAction("ViewTopics", new { topicSortOrder = topicSortOrder, questionSortOrder = questionSortOrder });
        }

        public ViewResult ViewTopics(Domain domain, string topicSortOrder, string questionSortOrder, int domainId)
        {
            if (domain.DomainId != 0)
            {
                ViewModel.Domain = domain;
                CurrentDomainId = domain.DomainId;
            }
            else
            {
                ViewModel.Domain = domainRepo.Domains.First(p => p.DomainId == CurrentDomainId);
            }

            ViewData["TNameSortParm"] = topicSortOrder == "name_asc" ? "name_dsc" : "name_asc";
            ViewData["TOrder"] = topicSortOrder == "OAsc" ? "ODsc" : "OAsc";
            ViewData["TActive"] = topicSortOrder == "Active" ? "Inactive" : "Active";
            ViewData["TId"] = topicSortOrder == "IAsc" ? "IDsc" : "IAsc";

            ViewData["QNameSortParm"] = questionSortOrder == "que_asc" ? "que_dsc" : "que_asc";
            ViewData["QOrder"] = questionSortOrder == "OAsc" ? "ODsc" : "OAsc";
            ViewData["QActive"] = questionSortOrder == "Active" ? "Inactive" : "Active";
            ViewData["QId"] = questionSortOrder == "IAsc" ? "IDsc" : "IAsc";

            var topics = topicRepo.Topics.Where(p => p.DomainId == ViewModel.Domain.DomainId);
            var questions = questionRepo.Questions.Where(p => topics.Contains(topicRepo.Topics.FirstOrDefault(q => q.TopicId == p.TopicId)));
            ViewBag.topicSortOrder = topicSortOrder;
            ViewBag.questionSortOrder = questionSortOrder;

            switch (topicSortOrder)
            {
                case "name_asc":
                    topics = topics.OrderBy(s => s.TopicName);
                    break;

                case "name_desc":
                    topics = topics.OrderByDescending(s => s.TopicName);
                    break;
                case "IDsc":
                    topics = topics.OrderByDescending(s => s.TopicId);
                    break;
                case "IAsc":
                    topics = topics.OrderBy(s => s.TopicId);
                    break;
                case "Active":
                    topics = topics.OrderBy(s => s.Active);
                    break;
                case "Inactive":
                    topics = topics.OrderByDescending(s => s.Active);
                    break;
                case "OAsc":
                    topics = topics.OrderBy(s => s.TopicOrder);
                    break;
                case "ODsc":
                    topics = topics.OrderByDescending(s => s.TopicOrder);
                    break;
                default:
                    topics = topics.OrderBy(s => s.TopicOrder);
                    break;
            }
            switch (questionSortOrder)
            {
                case "que_asc":
                    questions = questions.OrderBy(s => s.QuestionPrompt);
                    break;

                case "que_desc":
                    questions = questions.OrderByDescending(s => s.QuestionPrompt);
                    break;
                case "IDsc":
                    questions = questions.OrderByDescending(s => s.QuestionId);
                    break;
                case "IAsc":
                    questions = questions.OrderBy(s => s.QuestionId);
                    break;
                case "Active":
                    questions = questions.OrderBy(s => s.Active);
                    break;
                case "Inactive":
                    questions = questions.OrderByDescending(s => s.Active);
                    break;
                case "OAsc":
                    questions = questions.OrderBy(s => s.QuestionOrder);
                    break;
                case "ODsc":
                    questions = questions.OrderByDescending(s => s.QuestionOrder);
                    break;
                default:
                    questions = questions.OrderBy(s => s.QuestionOrder);
                    break;
            }

            ViewModel.Topics = topics.ToList();
            ViewModel.Questions = questions.ToList();
            return View(ViewModel);

        }
        
    }
}

        


