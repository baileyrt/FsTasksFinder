using ExplorationConsole.Properties;
using Gedcomx.Api.Lite;
using Gx.Common;
using Gx.Fs.Tree;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExplorationConsole
{
    class Program
    {
        public static JsonSerializerSettings jsettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore // Trims the extra content not needed in this case making things even faster with less content.
        };
        private static int TaskCount = 0;

        static void Main(string[] args)
        {
            //Example1();

            //Exploration();

            FindTasks();

            //var ft = CreateConnection(Gedcomx.Api.Lite.Environment.Integration);
            //var personId = "L5ND-K3D";
            //TraverseTree(ft, personId, 1);

            //TODO: read in username and password from the console
            //TODO: make this work in PROD
            //TODO: email a report with the results
            //    include links in the email, so users can just click things

            Console.WriteLine("Processing complete. Found {0} tasks", TaskCount);
            Console.ReadLine();
        }

        private static void TraverseTree(FamilySearchSDK ft, string personId, int generationCount)
        {
            //end the recursion at 5 generations
            if (generationCount > 5)
            {
                return;
            }
            var parsingResult = new RecordParsingResult();
            var personDetailUrl = "/platform/tree/persons/";
            var personHintsUrl = "/platform/tree/persons/{0}/matches?collection=https://familysearch.org/platform/collections/records";

            //fetch the person
            var response = ft.Get(personDetailUrl + personId).Result;
            //Console.WriteLine(response.ToString());
            //look for tasks in the person's data
            EvaluatePersonDetails(response, parsingResult);
            if (!parsingResult.IsLiving)
                Console.WriteLine(parsingResult.DetailSuggestions);
            else
                Console.WriteLine("No suggestions for records of living people");
            //look for tasks in the person's parents
            EvaluatePersonParents(response, parsingResult);
            Console.WriteLine(parsingResult.ParentSuggestions);
            //no record hints for living people
            if (!parsingResult.IsLiving)
            {
                //fetch hints
                var matchSearch = ft.Get(String.Format(personHintsUrl, personId)).Result;
                //look for tasks in the hints
                EvaluatePersonHints(matchSearch, parsingResult);
                if (!String.IsNullOrWhiteSpace(parsingResult.HintSuggestions))
                    Console.WriteLine(parsingResult.HintSuggestions);
                //evaluate children and their spouses for tasks
                if (parsingResult.ChildrenIds != null && parsingResult.ChildrenIds.Count > 0)
                {
                    foreach (var childId in parsingResult.ChildrenIds)
                    {
                        var childParsing = new RecordParsingResult();
                        var childResponse = ft.Get(personDetailUrl + childId).Result;
                        EvaluatePersonDetailsShallow(childResponse, childParsing);
                        if (!childParsing.IsLiving)
                        {
                            Console.WriteLine(childParsing.DetailSuggestions);
                            if (childParsing.SpouseIds != null && childParsing.SpouseIds.Count > 0)
                            {
                                foreach (var inlawId in childParsing.SpouseIds)
                                {
                                    var inlawParsing = new RecordParsingResult();
                                    var inlawResponse = ft.Get(personDetailUrl + inlawId).Result;
                                    EvaluatePersonDetailsShallow(inlawResponse, inlawParsing);
                                    if (!inlawParsing.IsLiving)
                                        Console.WriteLine(inlawParsing.DetailSuggestions);
                                }
                            }
                        }
                    }
                }
                if (parsingResult.SpouseIds != null && parsingResult.SpouseIds.Count > 0)
                {
                    foreach (var spouseId in parsingResult.SpouseIds)
                    {
                        var spouseParsing = new RecordParsingResult();
                        var spouseResponse = ft.Get(personDetailUrl + spouseId).Result;
                        EvaluatePersonDetailsShallow(spouseResponse, spouseParsing);
                        if (!spouseParsing.IsLiving)
                            Console.WriteLine(spouseParsing.DetailSuggestions);
                    }
                }
            }
            Console.WriteLine();

            //start recursion
            if (!String.IsNullOrWhiteSpace(parsingResult.FatherId))
                TraverseTree(ft, parsingResult.FatherId, generationCount + 1);
            if (!String.IsNullOrWhiteSpace(parsingResult.MotherId))
                TraverseTree(ft, parsingResult.MotherId, generationCount + 1);
        }

        private static void FindTasks()
        {
            var parsingResult = new RecordParsingResult();
            var fatherParsing = new RecordParsingResult();
            var motherParsing = new RecordParsingResult();
            var personDetailUrl = "/platform/tree/persons/";
            var personHintsUrl = "/platform/tree/persons/{0}/matches?collection=https://familysearch.org/platform/collections/records";

            //Israel Hoyt Heaton
            //var personId = "L5ND-KKY";
            //https://integration.familysearch.org/tree/person/details/L5ND-KKY
            //Israel Hoyt
            //var personId = "L5ND-KKB";
            //https://integration.familysearch.org/tree/person/details/L5ND-KKB
            //Kash M Johnson
            //var personId = "L5N4-17L";
            //https://integration.familysearch.org/tree/person/details/L5N4-17L
            //Ari M Havelock
            var personId = "L5N4-17V";
            //https://integration.familysearch.org/tree/person/details/L5N4-17V
            //Alma Heaton
            //var personId = "L5ND-K3D";

            var ft = CreateConnection(Gedcomx.Api.Lite.Environment.Integration);
            var response = ft.Get(personDetailUrl + personId).Result;
            //Console.WriteLine(response.ToString());
            EvaluatePersonDetails(response, parsingResult);
            if (!parsingResult.IsLiving)
                Console.WriteLine(parsingResult.DetailSuggestions);
            else
                Console.WriteLine("No suggestions for records of living people");
            EvaluatePersonParents(response, parsingResult);
            Console.WriteLine(parsingResult.ParentSuggestions);
            var matchSearch = ft.Get(String.Format(personHintsUrl, personId)).Result;
            EvaluatePersonHints(matchSearch, parsingResult);
            if (!String.IsNullOrWhiteSpace(parsingResult.HintSuggestions))
                Console.WriteLine(parsingResult.HintSuggestions);
            Console.WriteLine();

            //crawl the father
            if (!String.IsNullOrWhiteSpace(parsingResult.FatherId))
            {
                var fatherResponse = ft.Get(personDetailUrl + parsingResult.FatherId).Result;
                EvaluatePersonDetails(fatherResponse, fatherParsing);
                if (!fatherParsing.IsLiving)
                    Console.WriteLine(fatherParsing.DetailSuggestions);
                else
                    Console.WriteLine("No suggestions for records of living people");
                EvaluatePersonParents(fatherResponse, fatherParsing);
                Console.WriteLine(fatherParsing.ParentSuggestions);
                var fatherMatch = ft.Get(String.Format(personHintsUrl, parsingResult.FatherId)).Result;
                EvaluatePersonHints(fatherMatch, fatherParsing);
                if (!String.IsNullOrWhiteSpace(fatherParsing.HintSuggestions))
                    Console.WriteLine(fatherParsing.HintSuggestions);
                Console.WriteLine();
            }

            //crawl the mother
            if (!String.IsNullOrWhiteSpace(parsingResult.MotherId))
            {
                var motherResponse = ft.Get(personDetailUrl + parsingResult.MotherId).Result;
                EvaluatePersonDetails(motherResponse, motherParsing);
                if (!motherParsing.IsLiving)
                    Console.WriteLine(motherParsing.DetailSuggestions);
                else
                    Console.WriteLine("No suggestions for records of living people");
                EvaluatePersonParents(motherResponse, motherParsing);
                Console.WriteLine(motherParsing.ParentSuggestions);
                var motherMatch = ft.Get(String.Format(personHintsUrl, parsingResult.MotherId)).Result;
                EvaluatePersonHints(motherMatch, motherParsing);
                if (!String.IsNullOrWhiteSpace(motherParsing.HintSuggestions))
                    Console.WriteLine(motherParsing.HintSuggestions);
                Console.WriteLine();
            }

            if (!parsingResult.IsLiving)
            {
                if (parsingResult.ChildrenIds != null && parsingResult.ChildrenIds.Count>0)
                {
                    foreach (var childId in parsingResult.ChildrenIds)
                    {
                        var childParsing = new RecordParsingResult();
                        var childResponse = ft.Get(personDetailUrl + childId).Result;
                        EvaluatePersonDetailsShallow(childResponse, childParsing);
                        if (!childParsing.IsLiving)
                        {
                            Console.WriteLine(childParsing.DetailSuggestions);
                            if (childParsing.SpouseIds!=null&&childParsing.SpouseIds.Count>0)
                            {
                                foreach (var inlawId in childParsing.SpouseIds)
                                {
                                    var inlawParsing = new RecordParsingResult();
                                    var inlawResponse = ft.Get(personDetailUrl + inlawId).Result;
                                    EvaluatePersonDetailsShallow(inlawResponse, inlawParsing);
                                    if (!inlawParsing.IsLiving)
                                        Console.WriteLine(inlawParsing.DetailSuggestions);
                                }
                            }
                        }
                    }
                }
                if (parsingResult.SpouseIds != null && parsingResult.SpouseIds.Count > 0)
                {
                    foreach (var spouseId in parsingResult.SpouseIds)
                    {
                        var spouseParsing = new RecordParsingResult();
                        var spouseResponse = ft.Get(personDetailUrl + spouseId).Result;
                        EvaluatePersonDetailsShallow(spouseResponse, spouseParsing);
                        if (!spouseParsing.IsLiving)
                            Console.WriteLine(spouseParsing.DetailSuggestions);
                    }
                }
            }
        }

        private static void EvaluatePersonHints(dynamic response, RecordParsingResult parsingResult)
        {
            var outputName = String.Format("{0} ({1})", parsingResult.Name, parsingResult.Id);
            if (response == null)
            {
                parsingResult.HintSuggestions = String.Format("For {0}, there are no record hints available",
                    outputName);
                return;
            }
            if (response.entries != null)
            {
                var count = response.entries.Count;
                if (count > 0)
                    TaskCount++;
                //there must be a "paging" limitation on the call
                if (count == 5)
                    parsingResult.HintSuggestions = String.Format("For {0}, there are {1} (or more) record hints available",
                        outputName, count);
                else if (count == 1)
                    parsingResult.HintSuggestions = String.Format("For {0}, there is {1} record hints available",
                        outputName, count);
                else if (count == 0)
                    parsingResult.HintSuggestions = String.Format("For {0}, there are no record hints available",
                        outputName);
                else
                    parsingResult.HintSuggestions = String.Format("For {0}, there are {1} hints available",
                        outputName, count);
            }
            else
            {
                parsingResult.HintSuggestions = String.Format("For {0}, there are no record hints available",
                    outputName);
            }
        }

        private static void EvaluatePersonParents(dynamic response, RecordParsingResult parsingResult)
        {
            var output = new StringBuilder();
            var outputName = String.Format("{0} ({1})", parsingResult.Name, parsingResult.Id);
            var introFormat = "For {0}, here are some relationship suggestions:";
            var introFormatNoSuggestions = "For {0}, there are no relationship suggestions at this time";
            output.AppendLine(String.Format(introFormat, outputName));
            var parentsRecorded = false;
            if (response.childAndParentsRelationships != null)
            {
                foreach (var relationship in response.childAndParentsRelationships)
                {
                    //if the person being evaluated is the child, then...
                    if (relationship.child != null && relationship.child.resourceId != null &&
                        relationship.child.resourceId.Value.Equals(parsingResult.Id))
                    {
                        parentsRecorded = true;
                        //these are the parents
                        if (relationship.father != null)
                            parsingResult.FatherId = relationship.father.resourceId;
                        else
                        {
                            output.AppendLine("\tNo father recorded");
                            TaskCount++;
                        }
                        if (relationship.mother != null)
                            parsingResult.MotherId = relationship.mother.resourceId;
                        else
                        {
                            output.AppendLine("\tNo mother recorded");
                            TaskCount++;
                        }
                    }
                    //else, these are the children; no suggestions needed
                }
                if (!String.IsNullOrWhiteSpace(parsingResult.FatherId)
                    && !String.IsNullOrWhiteSpace(parsingResult.MotherId))
                {
                    parsingResult.ParentSuggestions = String.Format(introFormatNoSuggestions, outputName);
                }
                else
                {
                    if (!parentsRecorded)
                    {
                        output.AppendLine("\tNo parents recorded");
                        TaskCount++;
                    }
                    parsingResult.ParentSuggestions = output.ToString();
                }
            }
            else
            {
                parsingResult.ParentSuggestions = "No relationships found";
                TaskCount++;
            }
        }

        private static void EvaluatePersonDetails(dynamic response, RecordParsingResult parsingResult)
        {
            var output = new StringBuilder();
            var introFormat = "For {0}, here are some detail suggestions:";
            var introFormatNoSuggestions = "For {0}, there are no detail suggestions at this time";
            var hasSuggestions = false;
            var name = String.Empty;
            var id = String.Empty;
            var outputName = String.Empty;
            var hasBirthDate = false;
            var hasDeathDate = false;
            var givenName = String.Empty;
            var surname = String.Empty;

            var personDetails = response.persons.First;
            if (personDetails != null)
            {
                id = personDetails.id;
                parsingResult.Id = id;
                parsingResult.IsLiving = Convert.ToBoolean(personDetails.living);
                if (personDetails.display != null)
                {
                    name = personDetails.display.name;
                    if (String.IsNullOrWhiteSpace(name))
                    {
                        output.AppendLine(String.Format(introFormat, id));
                        output.AppendLine("\tNo name recorded");
                        TaskCount++;
                        hasSuggestions = true;
                    }
                    else
                    {
                        outputName = String.Format("{0} ({1})", name, id);
                        output.AppendLine(String.Format(introFormat, outputName));
                        parsingResult.Name = name;
                    }
                    hasBirthDate = !String.IsNullOrWhiteSpace(personDetails.display.birthDate.Value);
                    if (!hasBirthDate)
                    {
                        output.AppendLine("\tNo birth date recorded");
                        TaskCount++;
                        hasSuggestions = true;
                    }
                    hasDeathDate = !String.IsNullOrWhiteSpace(personDetails.display.deathDate.Value);
                    if (!hasDeathDate)
                    {
                        output.AppendLine("\tNo death date recorded");
                        TaskCount++;
                        hasSuggestions = true;
                    }
                    if (personDetails.display.familiesAsParent != null)
                    {
                        foreach (var familyContainer in personDetails.display.familiesAsParent)
                        {
                            if (familyContainer != null)
                            {
                                parsingResult.SpouseIds = new List<string>();
                                //store the spouse
                                //look in both "parent1" and "parent2" for the OTHER parent
                                if (familyContainer.parent1 != null)
                                {
                                    var parent = familyContainer.parent1.resourceId.Value;
                                    if (!parent.Equals(id))
                                        parsingResult.SpouseIds.Add(parent);
                                }
                                if (familyContainer.parent2 != null)
                                {
                                    var parent = familyContainer.parent2.resourceId.Value;
                                    if (!parent.Equals(id))
                                        parsingResult.SpouseIds.Add(parent);
                                }
                                //store the children
                                var childrenList = familyContainer.children;
                                if (childrenList != null)
                                {
                                    parsingResult.ChildrenIds = new List<string>();
                                    foreach (var child in childrenList)
                                    {
                                        parsingResult.ChildrenIds.Add(child.resourceId.Value);
                                    }
                                }
                            }
                        }
                    }
                } //end personDetails.display
                if (personDetails.names != null)
                {
                    var fullName = personDetails.names.First;
                    if (fullName.nameForms != null)
                    {
                        var nameForm = fullName.nameForms.First;
                        if (nameForm.parts != null)
                        {
                            foreach (var namePart in nameForm.parts)
                            {
                                if (namePart.type.Value.Equals("http://gedcomx.org/Given"))
                                    givenName = namePart.value.Value;
                                if (namePart.type.Value.Equals("http://gedcomx.org/Surname"))
                                    surname = namePart.value.Value;
                            }
                            if (String.IsNullOrWhiteSpace(givenName))
                            {
                                output.AppendLine("\tNo given name recorded");
                                TaskCount++;
                                hasSuggestions = true;
                            }
                            if (String.IsNullOrWhiteSpace(surname))
                            {
                                output.AppendLine("\tNo surname recorded");
                                TaskCount++;
                                hasSuggestions = true;
                            }
                        }
                    }
                } //end personDetails name parts
            } //end personDetails not null
            else
                parsingResult.DetailSuggestions = "Unable to process record";
            if (!hasSuggestions)
                parsingResult.DetailSuggestions = String.Format(introFormatNoSuggestions, outputName);
            else
                parsingResult.DetailSuggestions = output.ToString();
        }

        private static void EvaluatePersonDetailsShallow(dynamic response, RecordParsingResult parsingResult)
        {
            //"shallow" means don't dive down to the person's spouse or children
            var output = new StringBuilder();
            var introFormat = "For {0}, here are some detail suggestions:";
            var introFormatNoSuggestions = "For {0}, there are no detail suggestions at this time";
            var hasSuggestions = false;
            var name = String.Empty;
            var id = String.Empty;
            var outputName = String.Empty;
            var hasBirthDate = false;
            var hasDeathDate = false;
            var givenName = String.Empty;
            var surname = String.Empty;

            var personDetails = response.persons.First;
            if (personDetails != null)
            {
                id = personDetails.id;
                parsingResult.Id = id;
                parsingResult.IsLiving = Convert.ToBoolean(personDetails.living);
                if (personDetails.display != null)
                {
                    name = personDetails.display.name;
                    if (String.IsNullOrWhiteSpace(name))
                    {
                        output.AppendLine(String.Format(introFormat, id));
                        output.AppendLine("\tNo name recorded");
                        TaskCount++;
                        hasSuggestions = true;
                    }
                    else
                    {
                        outputName = String.Format("{0} ({1})", name, id);
                        output.AppendLine(String.Format(introFormat, outputName));
                        parsingResult.Name = name;
                    }
                    hasBirthDate = !String.IsNullOrWhiteSpace(personDetails.display.birthDate.Value);
                    if (!hasBirthDate)
                    {
                        output.AppendLine("\tNo birth date recorded");
                        TaskCount++;
                        hasSuggestions = true;
                    }
                    hasDeathDate = !String.IsNullOrWhiteSpace(personDetails.display.deathDate.Value);
                    if (!hasDeathDate)
                    {
                        output.AppendLine("\tNo death date recorded");
                        TaskCount++;
                        hasSuggestions = true;
                    }
                } //end personDetails.display
                if (personDetails.names != null)
                {
                    var fullName = personDetails.names.First;
                    if (fullName.nameForms != null)
                    {
                        var nameForm = fullName.nameForms.First;
                        if (nameForm.parts != null)
                        {
                            foreach (var namePart in nameForm.parts)
                            {
                                if (namePart.type.Value.Equals("http://gedcomx.org/Given"))
                                    givenName = namePart.value.Value;
                                if (namePart.type.Value.Equals("http://gedcomx.org/Surname"))
                                    surname = namePart.value.Value;
                            }
                            if (String.IsNullOrWhiteSpace(givenName))
                            {
                                output.AppendLine("\tNo given name recorded");
                                TaskCount++;
                                hasSuggestions = true;
                            }
                            if (String.IsNullOrWhiteSpace(surname))
                            {
                                output.AppendLine("\tNo surname recorded");
                                TaskCount++;
                                hasSuggestions = true;
                            }
                        }
                    }
                } //end personDetails name parts
            } //end personDetails not null
            else
                parsingResult.DetailSuggestions = "Unable to process record";
            if (!hasSuggestions)
                parsingResult.DetailSuggestions = String.Format(introFormatNoSuggestions, outputName);
            else
                parsingResult.DetailSuggestions = output.ToString();
        }

        private static FamilySearchSDK CreateConnection(Gedcomx.Api.Lite.Environment env)
        {
            var username = String.Empty;
            var password = String.Empty;
            var applicationKey = "a02f100000TbRwqAAF";
            switch (env)
            {
                case Gedcomx.Api.Lite.Environment.Integration:
                    username = "tum000141673";
                    password = "nope";
                    break;
                case Gedcomx.Api.Lite.Environment.Production:
                    username = "baileyrttest";
                    password = "nope";
                    break;
                default:
                    return null;
            }
            return new FamilySearchSDK(username, password, applicationKey, "TasksFinder", "1.0.0", env);
        }

        private static void Exploration()
        {
            //pepere bailey
            //var personId = "L899-5WF";
            var personId = "L5ND-KKY";
            //production?
            //var username = "baileyrttest";
            //var password = "nope";
            //integration
            var username = "tum000141673";
            var password = "nope";

            var applicationKey = "a02f100000TbRwqAAF";
            //integration
            var ft = new FamilySearchSDK(username, password, applicationKey, "SampleConsoleApp", "1.0.0", Gedcomx.Api.Lite.Environment.Integration);
            //production
            //var ft = new FamilySearchSDK(username, password, applicationKey, "SampleConsoleApp", "1.0.0", Gedcomx.Api.Lite.Environment.Production);

            //404 error
            var response = ft.Get("/platform/tree/persons/" + personId).Result;

            var encoded = Uri.EscapeDataString("motherGivenName:Clarissa~ fatherSurname:Heaton~ motherSurname:Hoyt~ surname:Heaton~ givenName:Israel~ fatherGivenName:Jonathan~");
            var searchResult = ft.Get("/platform/tree/search?q=" + encoded, MediaType.X_GEDCOMX_ATOM_JSON).Result;
            //Console.WriteLine($"Found close hits {searchResult.searchInfo[0].closeHits} with {searchResult.searchInfo[0].totalHits} total");

            var matchSearch = ft.Get("/platform/tree/persons/L5ND-KKY/matches?collection=https://familysearch.org/platform/collections/records").Result;
            Console.WriteLine(matchSearch.ToString());
        }        
    }
}
