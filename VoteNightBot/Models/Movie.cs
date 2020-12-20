using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using Discord;
using OMDbSharp.Objects;
using VoteNightBot.Services;

namespace VoteNightBot.Models
{
    class Movie
    {
        [Key]
        public string ID { get; set; }
        public string Title { get; set; }
        public int VoteCount { get; set; }

        public static Embed GetMovieEmbed(Item movie)
        {
            var builder = new EmbedBuilder();
            if (!string.IsNullOrWhiteSpace(movie.Title))
                builder.WithTitle(movie.Title);
            else
            {
                builder.WithTitle("Movie not found");
                return builder.Build();
            }
            if(Uri.TryCreate(movie.Poster, UriKind.Absolute, out var uri))
                builder.WithImageUrl(movie.Poster);
            if (!string.IsNullOrWhiteSpace(movie.Title))
                builder.AddField("Genre", movie.Genre, false);
            if (!string.IsNullOrWhiteSpace(movie.IMDbRating))
                builder.AddField("Rating", movie.IMDbRating, true);
            if (!string.IsNullOrWhiteSpace(movie.Director))
                builder.AddField("Directors", movie.Director, true);
            if (!string.IsNullOrWhiteSpace(movie.Writer))
                builder.AddField("Writer", movie.Writer, true);
            if (!string.IsNullOrWhiteSpace(movie.Year))
                builder.AddField("Year", movie.Year, true);
            if (!string.IsNullOrWhiteSpace(movie.Plot))
                builder.AddField("Plot", movie.Plot, false);
            if (!string.IsNullOrWhiteSpace(movie.IMDbID))
            {
                builder.WithUrl($"https://www.imdb.com/title/{movie.IMDbID}");
                builder.WithFooter($"https://www.imdb.com/title/{movie.IMDbID}", String.Empty);
            }
            return builder.Build();
        }
        
    }
}
