# Trello Automation - A HUGO Site helper
Create HUGO Pages From Trello Cards

## High Level Process Summary
1. Reads trello cards from a list you define on a board you define.
2. Pulls data from the card and writes it to a .md for HUGO page builder
3. Sends pictured related to the article to TinyPNG to be optimzed and alterted.
4. Quicky creates ready to deploy images and .md files for your HUGO website.

## Requirements
* Trello account
* Trello API Key - Can be found here https://trello.com/app-key
* Trello Token - Can be found at the link above ^^^ by clicking "Token" hyperlink. You will have to authorize Trello to get the token.
* TinyPNG account - free account and api key https://tinypng.com/

## Program Variable - Set the following

```
string apiKey = "";//Replace with your Trello API Key
string tokenKey = "";// Replace with your Trello Auth Token
Tinify.Key = ""; // Replace with tinypng API key
string SourceBoardName = "";//Source Board as it is shown (Case Sensitive)
string SourceListName = "";//Source List as it is shown (Case Sensitive)

string PostFileOutput = @"C:\TrelloOutput\";//Where the .md Hugo files will be generated.
string ImgFileOutput = @"C:\TrelloOutput\Completed_Images\";//Where the compressed optimized images will be sent
```
            
## Setting Up Trello
After you have defined your board and list the program will iterate through the list on the board you set. Each card in the list will be pulled with the following information.
* Name
* Due Date
* Labels
* Description
* Last Comment - **Place the Raw Markdown of the article text you plan on using in the comment** Trello also uses markdown so the way it looks on the comment is they way it will look through HUGO 

All of these will be used in creating the .md file for Hugo. The card description houses various additional terms such as:

* Keyword the article is targeting
* Monthly Search Volume
* Meta Description
* NOTE: the card label is in there for when I import cards but it is not required.

### Example of the markdown to use in the description area of Trello Cards(Use this as a template because the program will fail otherwise.
```
Keyword:
-----------------------
What is a DBA
.
Monthly Searches:
-----------------------
 3030
.
META Desc: 
-----------------------
meta description that will be what google uses
.
Tech Artcles (Space for HUGO article Category/tags)
```

## Using The Image Optimization

Make sure Both variable are set:
* PostFileOutput
* ImgFileOutput

The original images should be placed in "PostFileOutput" location and have the following filename mask.
``` 
keyword-with-spaces-as-seen-in-trello-card-desc.jpg 
```

The example used above: the keyword is "what is a dba" the following file should be named accordingly.
This is assuming your HUGO theme supports both a featured image and a thumbnail image.
* Featured - "what-is-a-dba.jpg"
* Thumbnail - "t-what-is-dba.jpg"

Featured image will be converted/optimized as is.
Thumbnail image will be converted then scaled down to 600x600 pixels. (uses the "cover" AI algorithm can be found here https://tinypng.com/developers/reference/dotnet)
