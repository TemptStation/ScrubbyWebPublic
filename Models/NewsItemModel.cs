using System;
using Markdig;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ScrubbyWeb.Models
{
    public class NewsItemModel
    {
        [BsonId] public ObjectId NewsItemID { get; set; }

        public DateTime Date { get; set; }
        public string Author { get; set; }
        public string Title { get; set; }
        public string RawContent { get; set; }

        public string ToHTML()
        {
            var pipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .Build();

            return Markdown.ToHtml(RawContent, pipeline);
        }
    }
}