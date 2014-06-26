using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;

namespace FactCheckerWPF
{
    public class TruthChecker
    {
        List<FactCheckData> factsToCheck;

        public TruthChecker()
        {
            factsToCheck = new List<FactCheckData>();
        }
        private string[] Actions = {
            "raise",
            "lower",
            "cut",
            "break",
            "reduce",
            "increase",
            "add",
            "adds",
            "spend",
            "kill",
            "fire",
            "decrease",
            "create",
            "created",
            "weaken",
            "weakened",
            "spend",
            "spends",
            "lose",
            "losing",
            "remove",
            "get",
            "take"
                                   };

        private string[] ProperNouns = {
            "obama",
            "obamas",
            "romneys",
            "romney",
            "aarp",
            "obamacare",
            "pbs"
                                       };

        private string[] Nouns = {
            "tax",
            "job",
            "military",
            "education",
            "medicare",
            "medicaid",
            "insurance",
            "employment",
            "unemployment",
            "employed",
            "unemployed",
            "economy",
            "government",
            "income",
            "health",
            "deficit",
            "big",
            "bird"
                                 };

        private string[] Numerical = {
            "million",
            "trillion",
            "billion"
                                     };

        private string[] KeyWords = {
            "romney",
            "obama",
            "tax",
            "taxes",
            "deficit",
            "military",
            "cut",
            "raise",
            "medicare",
            "medicaid",
            "trillion",
            "billion",
            "million",
            "pbs",
            "big bird",
            "end",
            "start",
            "increase",
            "reduce",
            "spending",
            "1",
            "2",
            "3",
            "4",
            "5",
            "6",
            "7",
                                    };

 
        // returns a string indicating how truthy the statement is
        public string CheckThis(string text, List<SentenceChunk> tagTokenList,StreamWriter writer, out List<FactCheckData> outList)
        {
            string outStr = "";
            outList = new List<FactCheckData>();

            // text is a result of dictation capture
            // first, find keywords
            string words = FindKeyWords(text, tagTokenList);
            //writer.WriteLine("spoken text " + text);
            //writer.Flush();
            //writer.WriteLine("words found " + words);
            //writer.Flush();
            //factsToCheck.Clear();

            // Check if there is more than 1 word to match
            if ((words.Length > 0) && words.Contains('+'))
            {

                // create string of words to send to URL and do magic

                // used to build entire input
                StringBuilder sb = new StringBuilder();

                // used on each read operation
                byte[] buf = new byte[16384];

                // build query
                string httpString = "http://www.politifact.com/search/?q=" + words;

                HttpWebRequest request = (HttpWebRequest)
                    WebRequest.Create(httpString);

                //Console.WriteLine(httpString);
                // execute the request
                HttpWebResponse response = (HttpWebResponse)
                    request.GetResponse();

                // we will read data via the response stream
                Stream resStream = response.GetResponseStream();
                string tempString = null;
                int count = 0;

                do
                {
                    // fill the buffer with data
                    count = resStream.Read(buf, 0, buf.Length);

                    // make sure we read some data
                    if (count != 0)
                    {
                        // translate from bytes to ASCII text
                        tempString = Encoding.ASCII.GetString(buf, 0, count);
                        string[] lines = tempString.Split('\n');

                        // continue building the string
                        sb.Append(tempString);
                        foreach (string line in lines)
                        {
                            if (System.Text.RegularExpressions.Regex.IsMatch(line, "class=\"statement\""))
                            {
                                //lineList.Add(line);
                                string outString;
                                decimal out1 = DetermineRelevancy(line, words, text, out outString);
                                string truth = DetermineTruthiness(line, words);

                                factsToCheck.Add(new FactCheckData { SearchText = outString, Truthiness = truth, Relevancy = out1, SpokenText = text });

                                if (Convert.ToDouble(out1) > 0.3)
                                {
                                    outList.Add(new FactCheckData { SearchText = outString, Truthiness = truth, Relevancy = out1, SpokenText = text });
                                }
                            }

                        }
                    }

                }
                while (count > 0); // any more data to read?

                // sort list by relevancy
                factsToCheck.Sort((a, b) => Decimal.Compare(a.Relevancy, b.Relevancy));
                factsToCheck.Reverse();
            }
            else
            {
                factsToCheck.Clear();
            }

            // item 0 should be the highest relevancy, if its relevancy score 1 then add it to the display

            if (factsToCheck.Count > 0)
            {
                if ( Convert.ToDouble(factsToCheck[0].Relevancy) > 0.6)
                {
                    outStr += "relevancy: " + factsToCheck[0].Relevancy + '\t';
                    outStr += "truthiness: " + factsToCheck[0].Truthiness + '\t';
                    outStr += factsToCheck[0].SearchText + "\n";
                }

                else
                {
                    outStr += "Checking inconclusive: " + factsToCheck[0].SpokenText;
                }
            }

            if (outStr == "")
            {
                outStr += "Checking inconclusive: " + text;
            }

            return outStr;

        }

        private string FindKeyWords(string text, List<SentenceChunk> tagTokenList)
        {
            
            
            string[] words = text.Split(' ');
            string outString = "";
            double iout;
            bool properNounFound = false;
            bool nounFound = false;
            bool actionFound = false;
            bool numericalFound = false;
            bool numberFound = false;
            int index = 0;
            foreach (SentenceChunk tagToken in tagTokenList)
            {
                string wordLc = tagToken.Word.ToLower().Trim();

                // word stemmer doesnt work; converts "raise" to "rais".. so use only in special cases
                if ((System.Text.RegularExpressions.Regex.IsMatch(tagToken.WordType, "NNS")) || 
                    (System.Text.RegularExpressions.Regex.IsMatch(tagToken.WordType, "VBG")))
                {
                    wordLc = new EnglishWord(wordLc).Stem;
                }
                
                
                // might be a good op for NLP - if -ing vb or plural noun then stem
                if (ProperNouns.Contains(wordLc))
                {
                    outString += wordLc + "+";
                    properNounFound = true;
                    tagToken.WordCategory = "ProperNoun";
                }
                else if (Nouns.Contains(wordLc))
                {
                    outString += wordLc + "+";
                    nounFound = true;
                    tagToken.WordCategory = "Noun";
                    
                }
                else if (Actions.Contains(wordLc))
                {
                    outString += wordLc + "+";
                    actionFound = true;
                    tagToken.WordCategory = "Action";

                }
                else if (Numerical.Contains(wordLc))
                {
                    outString += wordLc + "+";
                    numericalFound = true;
                    tagToken.WordCategory = "Numerical";
                }
                else if ((wordLc.Length > 0) && (double.TryParse(wordLc, out iout)))
                {
                    outString += wordLc + "+";
                    numberFound = true;
                    tagToken.WordCategory = "Number";
                }

                index++;
            }
            /*
            if (!(actionFound && nounFound))
            {
                // dont have an action and a object, return empty
                return "action and noun not found.  action " + actionFound.ToString() + "noun " + nounFound.ToString();
            }
            */
            // remove final +
            if (outString.Length > 0)
            {
                outString = outString.Substring(0, outString.Length - 1);
            }

            return outString;
        }

        decimal DetermineRelevancy(string lineWeb, string words, string textInput, out string subStr)
        {
            if (words.Length == 0)
            {
                subStr = "";
                return 0;
            }

            int inputTextCount = textInput.Split(' ').Length;

            int start = lineWeb.IndexOf("a href");
            if (start == -1)
            {
                // line got chopped; return 0
                subStr = "";
                return 0;
            }

            int end = lineWeb.IndexOf(">", start);

            if (end == -1)
            {
                // line got chopped; return 0
                subStr = "";
                return 0;
            }
            int len = end - start;
            subStr = lineWeb.Substring(start + 7, len - 7);

            // now split substr by "/"
            string[] data = subStr.Split('/');

            // last data point will be the page (keyword search)
            string searchStr = data[data.Length - 2];


            // just do this
            string[] words2 = words.Split('+');
            int wordsCount = words2.Length;
            int relevancyScore = 0;
            foreach (string word in words2)
            {
                if (System.Text.RegularExpressions.Regex.IsMatch(searchStr, word))
                {
                    relevancyScore++;
                }
            }
            decimal count = relevancyScore;
            decimal length = words2.Length;
            decimal res = 0;

            // what if we get a good match but the wrong guy!? check for this? or display the factoid?
            //if (wordsCount > 2)
            //{
                // sufficient words for a match, calculate relevency
                res = Math.Round(count / length, 2);
            //}
            return res;
        }

        string DetermineTruthiness(string line, string textInput)
        {
            if (textInput.Length == 0)
            {
                return "";
            }

            string subStr;
            string outStr = "";
            int start = line.IndexOf("Sort order");

            if (start == -1)
            {
                return "";
            }

            //int end = line.IndexOf('"', start + 8);
            subStr = line.Substring(start, 13);

            // just do this
            string sortVal = subStr.Substring(subStr.Length - 2);
            sortVal = sortVal.Trim();

            decimal sortDec = Convert.ToInt32(sortVal);

            switch (sortVal)
            {
                case "1":
                    outStr = "truthy";
                    break;
                case "2":
                    outStr = "mostly truthy";
                    break;
                case "3":
                    outStr = "half truthy";
                    break;
                case "4":
                    outStr = "not quite truthy";
                    break;
                case "5":
                    outStr = "highly untruthy";
                    break;
                case "6":
                    outStr = "untruthy!";
                    break;
                default:
                    break;
            }

            return outStr;
        }
    }
}
