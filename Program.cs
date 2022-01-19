using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using RestSharp;
using RestSharp.Authenticators;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using TinifyAPI;
using Microsoft.Extensions.Configuration;

namespace TrelloAutomation
{
    class Program
    {
        static async Task Main(string[] args)
        {
            //reads API info from protected file
            var config = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory).AddJsonFile("appsettings.json").AddUserSecrets<Program>().Build();

            //Trello API info
            string apiKey = config.GetValue<string>("ReadSecrets:TrelloAPI");//Replace with your Trello API Key
            string tokenKey = config.GetValue<string>("ReadSecrets:TrelloToken");// Replace with your Trello Auth Token
            Tinify.Key = config.GetValue<string>("ReadSecrets:TinyPngAPI"); // Replace with tinypng API key
            string SourceBoardName = "";//Source Board as it is shown (Case Sensitive)
            string SourceListName = "";//Source List as it is shown (Case Sensitive)
            string SourceBoardID = "";//Leave Empty
            string SourceListID = "";//Leave Empty


            string PostFileOutput = @"C:\TrelloOutput\";//Where the .md Hugo files will be generated.
            string ImgFileOutput = @"C:\TrelloOutput\Completed_Images\";//Where the compressed optimized images will be sent

            var client = new RestClient("https://api.trello.com/1/");

            //This request fetches all the boards
            var requestBoards = new RestRequest("members/me/boards?key=" + apiKey + "&token=" + tokenKey);
            var BoardsResponse = await client.GetAsync(requestBoards);
            var ListBoards = JsonConvert.DeserializeObject<IList<Boards>>(BoardsResponse.Content);

            foreach (var board in ListBoards)//Search all the boards for one I want
            {
                if (board.name == SourceBoardName)
                {
                    SourceBoardID = board.id;
                    Console.WriteLine("Trello Source Board Found");
                    break;
                }
            }

            //This request fetches all the lists under the source board.
            var requestLists = new RestRequest("boards/" + SourceBoardID + "/lists/?key=" + apiKey + "&token=" + tokenKey);
            var ListsResponse = await client.GetAsync(requestLists);
            var ListLists = JsonConvert.DeserializeObject<IList<Lists>>(ListsResponse.Content);

            foreach (var list in ListLists)//Search The Board for the List I want
            {
                if (list.name == SourceListName)
                {
                    SourceListID = list.id;
                    Console.WriteLine("Trello Source List Found");
                    break;
                }
            }

            //This request fetches all the cards in the source list on the source board
            var requestCards = new RestRequest("lists/" + SourceListID + "/cards/?key=" + apiKey + "&token=" + tokenKey);
            var CardsResponse = await client.GetAsync(requestCards);
            var ListCards = JsonConvert.DeserializeObject<IList<Card>>(CardsResponse.Content);

            foreach (var card in ListCards)//loops through pulling out card data
            {
                string[] CardDescLines = card.desc.Split(new string[] { "\n" }, StringSplitOptions.None);
                DateTime cardDueFullDate = Convert.ToDateTime(card.due);
                string cardDueDate = cardDueFullDate.ToString("yyyy-MM-dd");
                var Keyword = CardDescLines[2];
                var Name = card.name;
                var META = CardDescLines[10];
                var Category = CardDescLines[12];
                bool featureImgCheck = false;
                bool thumbNailCheck = false;

                //Logic to compress the featured image
                foreach (string file in Directory.EnumerateFiles(PostFileOutput, Keyword.Replace(" ", "-") + ".jpg", SearchOption.AllDirectories))
                {
                    featureImgCheck = true;
                    try
                    {
                        var FeaturedSource = Tinify.FromFile(PostFileOutput + Keyword.Replace(" ", "-") + ".jpg");
                        await FeaturedSource.ToFile(ImgFileOutput + Keyword.Replace(" ", "-") + ".jpg");
                        File.Delete(PostFileOutput + Keyword.Replace(" ", "-") + ".jpg");//Delete original photo
                        featureImgCheck = true;
                    }
                    catch { break; }
                }

                //Logic to compress and alter the Thumbnail image
                foreach (string file in Directory.EnumerateFiles(PostFileOutput, "t-" + Keyword.Replace(" ", "-") + ".jpg", SearchOption.AllDirectories))
                {
                    thumbNailCheck = true;
                    try
                    {
                        var thumbSource = Tinify.FromFile(PostFileOutput + "t-" + Keyword.Replace(" ", "-") + ".jpg");
                        var resized = thumbSource.Resize(new //After optimization alter image to fixed size
                        {
                            method = "cover",
                            width = 600,
                            height = 600
                        });
                        await resized.ToFile(ImgFileOutput + "t-" + Keyword.Replace(" ", "-") + ".jpg");
                        File.Delete(PostFileOutput + "t-" + Keyword.Replace(" ", "-") + ".jpg");//Delet original photo
                        thumbNailCheck = true;
                    }
                    catch { break; }
                }

                if (featureImgCheck == false && thumbNailCheck == false)//This is a fallback incase the image names are wrong.
                {
                    Console.WriteLine("No images found for " + Keyword);
                    Console.WriteLine("");
                    Console.WriteLine("PLEASE CLOSE AND RESTART");
                    Console.WriteLine("");
                    Console.ReadLine();
                }

                //This request fetches the comments on each card (i put the raw Markdown for article in comment section)
                var requestComment = new RestRequest("/cards/"+card.id+"/actions?key=" + apiKey + "&token=" + tokenKey);
                var CommentResponse = await client.GetAsync(requestComment);
                var ListComment = JsonConvert.DeserializeObject<IList<Comment>>(CommentResponse.Content);

                foreach (var comment in ListComment)
                {
                    if (comment.data.text != null)
                    {
                        //Calls on file writer method
                        WriteFile(Keyword, Name, META, cardDueDate, Category, comment.data.text, PostFileOutput);
                        break;
                    }
                }
            }
        }

        public static void WriteFile(string keyword, string name, string meta, string date, string category, string articleText, string filePath)
        {//This generates the .md file for HUGO
            string fileline1 = "---";
            string fileline2 = "title: \"" + name + "\"";
            string fileline3 = "date: " + date;
            string fileline4 = "description: \"" + meta + "\"";
            string fileline5 = "featured: false";
            string fileline6 = "draft: false";
            string fileline7 = "toc: false";
            string fileline8 = "# menu: main";
            string fileline9 = "usePageBundles: false";
            string fileline10 = "featureImage: \"/images/" + keyword.Replace(" ", "-") + ".jpg" + "\"";
            string fileline11 = "featureImageAlt: \"Description of image\"";
            string fileline12 = "#featureImageCap:";
            string fileline13 = "thumbnail: \"/images/" + "t-" + keyword.Replace(" ", "-") + ".jpg" + "\"";
            string fileline14 = "shareImage: \"/images/" + "t-" + keyword.Replace(" ", "-") + ".jpg" + "\"";
            string fileline15 = "codeMaxLines: 10";
            string fileline16 = "codeLineNumbers: false";
            string fileline17 = "figurePositionShow: true";
            string fileline18 = "categories:";
            string fileline19 = "- " + category;
            string fileline20 = "# comment: false # Disable comment if false.";
            string fileline21 = "---";
            string fileline22 = "";


            File.Delete(filePath + keyword.Replace(" ", "-") + ".md");
            File.AppendAllText(filePath + keyword.Replace(" ", "-") + ".md", fileline1 + Environment.NewLine);
            File.AppendAllText(filePath + keyword.Replace(" ", "-") + ".md", fileline2 + Environment.NewLine);
            File.AppendAllText(filePath + keyword.Replace(" ", "-") + ".md", fileline3 + Environment.NewLine);
            File.AppendAllText(filePath + keyword.Replace(" ", "-") + ".md", fileline4 + Environment.NewLine);
            File.AppendAllText(filePath + keyword.Replace(" ", "-") + ".md", fileline5 + Environment.NewLine);
            File.AppendAllText(filePath + keyword.Replace(" ", "-") + ".md", fileline6 + Environment.NewLine);
            File.AppendAllText(filePath + keyword.Replace(" ", "-") + ".md", fileline7 + Environment.NewLine);
            File.AppendAllText(filePath + keyword.Replace(" ", "-") + ".md", fileline8 + Environment.NewLine);
            File.AppendAllText(filePath + keyword.Replace(" ", "-") + ".md", fileline9 + Environment.NewLine);
            File.AppendAllText(filePath + keyword.Replace(" ", "-") + ".md", fileline10 + Environment.NewLine);
            File.AppendAllText(filePath + keyword.Replace(" ", "-") + ".md", fileline11 + Environment.NewLine);
            File.AppendAllText(filePath + keyword.Replace(" ", "-") + ".md", fileline12 + Environment.NewLine);
            File.AppendAllText(filePath + keyword.Replace(" ", "-") + ".md", fileline13 + Environment.NewLine);
            File.AppendAllText(filePath + keyword.Replace(" ", "-") + ".md", fileline14 + Environment.NewLine);
            File.AppendAllText(filePath + keyword.Replace(" ", "-") + ".md", fileline15 + Environment.NewLine);
            File.AppendAllText(filePath + keyword.Replace(" ", "-") + ".md", fileline16 + Environment.NewLine);
            File.AppendAllText(filePath + keyword.Replace(" ", "-") + ".md", fileline17 + Environment.NewLine);
            File.AppendAllText(filePath + keyword.Replace(" ", "-") + ".md", fileline18 + Environment.NewLine);
            File.AppendAllText(filePath + keyword.Replace(" ", "-") + ".md", fileline19 + Environment.NewLine);
            File.AppendAllText(filePath + keyword.Replace(" ", "-") + ".md", fileline20 + Environment.NewLine);
            File.AppendAllText(filePath + keyword.Replace(" ", "-") + ".md", fileline21 + Environment.NewLine);
            File.AppendAllText(filePath + keyword.Replace(" ", "-") + ".md", fileline22 + Environment.NewLine);
            File.AppendAllText(filePath + keyword.Replace(" ", "-") + ".md", articleText);
        }

        public class ReadSecrets//read API info from secret.json file
        {
            public string TrelloAPI { get; set; }
            public string TrelloToken { get; set; }
            public string TinyPngAPI { get; set; }
        }

        public class Card
        {
            public string id { get; set; }
            public string desc { get; set; }
            public string name { get; set; }
            public string due { get; set; }
        }

        public class Boards
        {
            public string id { get; set; }
            public string name { get; set; }
        }

        public class Lists
        {
            public string id { get; set; }
            public string name { get; set; }
        }

        public class Comment
        {
            public string id { get; set; }
            public string idMemberCreator { get; set; }
            public CommentContainer data { get; set; }
        }

        public class CommentContainer
        {
            public string text { get; set; }
        }
    }
}