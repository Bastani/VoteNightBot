﻿using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using VoteNightBot.Models;
using VoteNightBot.Services;

namespace VoteNightBot
{
    public class CommandHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commands;

        // Retrieve client and CommandService instance via ctor
        public CommandHandler(DiscordSocketClient client, CommandService commands)
        {
            _commands = commands;
            _client = client;
        }

        public async Task InstallCommandsAsync()
        {
            // Hook the MessageReceived event into our command handler
            _client.MessageReceived += HandleCommandAsync;

            // Here we discover all of the command modules in the entry 
            // assembly and load them. Starting from Discord.NET 2.0, a
            // service provider is required to be passed into the
            // module registration method to inject the 
            // required dependencies.
            //
            // If you do not use Dependency Injection, pass null.
            // See Dependency Injection guide for more information.
            await _commands.AddModulesAsync(assembly: Assembly.GetEntryAssembly(),
                                            services: null);
        }

        private async Task HandleCommandAsync(SocketMessage messageParam)
        {
            // Don't process the command if it was a system message
            var message = messageParam as SocketUserMessage;
            if (message == null) return;

            // Create a number to track where the prefix ends and the command begins
            int argPos = 0;

            // Determine if the message is a command based on the prefix and make sure no bots trigger commands
            if (!(message.HasCharPrefix('!', ref argPos) ||
                message.HasMentionPrefix(_client.CurrentUser, ref argPos)) ||
                message.Author.IsBot)
                return;

            // Create a WebSocket-based command context based on the message
            var context = new SocketCommandContext(_client, message);

            // Execute the command with the command context we just
            // created, along with the service provider for precondition checks.
            await _commands.ExecuteAsync(
                context: context,
                argPos: argPos,
                services: null);
        }
    }

    [Group("votenight")]
    public class MovieModule : ModuleBase<SocketCommandContext>
    {
        [Command("search")]
        [Summary("Searches a movie on IMDB.")]
        public async Task SearchAsync([Summary("The movie to search.")][Remainder] string movieString)
        {
            var client = new OMDbSharp.OMDbClient(Environment.GetEnvironmentVariable("OMDbAPI", EnvironmentVariableTarget.User), true);
            var movie = await client.GetItemByTitle(movieString);
            
            await Context.Channel.SendMessageAsync(string.Empty, embed: Movie.GetMovieEmbed(movie));
        }

        [Command("vote")]
        [Summary("Adds a movie to database.")]
        public async Task AddAsync([Summary("The movie to add.")][Remainder] string movieString)
        {
            var client = new OMDbSharp.OMDbClient(Environment.GetEnvironmentVariable("OMDbAPI", EnvironmentVariableTarget.User), true);
            var movie = await client.GetItemByTitle(movieString);

            var localContext = new SqliteContext();
            var localUser = await localContext.User.FindAsync(Context.User.Id.ToString());
            if (localUser == null)
            {
                localUser = new User()
                {
                    ID = Context.User.Id.ToString()
                };
                await localContext.User.AddAsync(localUser);
            }

            if (!localUser.Voted)
            {
                var localMovie = await localContext.Movie.FindAsync(movie.IMDbID);
                if (localMovie == null)
                {
                    localMovie = new Movie()
                    {
                        ID = movie.IMDbID,
                        Title = movie.Title
                    };
                    await localContext.Movie.AddAsync(localMovie);
                }

                localUser.Voted = true;
                localUser.MoviePickedId = localMovie.ID; 
                localMovie.VoteCount++;

                await Context.Channel.SendMessageAsync($"Voted for movie: {movie.Title}", embed:Movie.GetMovieEmbed(movie));
            }
            else
            {
                await Context.Channel.SendMessageAsync($"Already voted for movie: {(await localContext.Movie.FindAsync(localUser.MoviePickedId)).Title}");
            }

            await localContext.SaveChangesAsync();
        }

        [Command("unvote")]
        [Summary("Removes vote from database")]
        public async Task UnvoteAsync()
        {
            var localContext = new SqliteContext();
            var localUser = await localContext.User.FindAsync(Context.User.Id.ToString());
            if (localUser == null)
            {
                localUser = new User()
                {
                    ID = Context.User.Id.ToString()
                };
                await localContext.User.AddAsync(localUser);
            }

            if (localUser.Voted)
            {
                var movieId = localUser.MoviePickedId;
                var localMovie = await localContext.Movie.FindAsync(movieId);
                localMovie.VoteCount--;

                localUser.Voted = false;
                localUser.MoviePickedId = string.Empty;

                await Context.Channel.SendMessageAsync($"User vote removed from movie: {(await localContext.Movie.FindAsync(movieId)).Title}");
            }
            else
            {
                await Context.Channel.SendMessageAsync($"User has not voted yet");
            }

            await localContext.SaveChangesAsync();
        }

        [Command("view")]
        [Summary("View all votes in Database")]
        public async Task ViewAsync()
        {
            var localContext = new SqliteContext();

            var movieList = localContext.Movie.ToList();

            if (movieList.Any())
            {

                var builder = new EmbedBuilder();
                builder.Title = "Movie List";
                foreach (var movie in movieList)
                {
                    builder.AddField(movie.Title, movie.VoteCount, true);
                }

                await Context.Channel.SendMessageAsync(string.Empty, embed: builder.Build());
            }
            else
            {
                await Context.Channel.SendMessageAsync("There are no movies");
            }
        }

        [Command("clear")]
        [RequireUserPermission(GuildPermission.Administrator, Group = "Permission")]
        [RequireOwner(Group = "Permission")]
        [Summary("Removes all votes from database")]
        public async Task ClearAsync()
        {
            var localContext = new SqliteContext();

            var movieList = localContext.Movie.ToList().Where(a => a.VoteCount > 0).ToList();
            var userList = localContext.User.ToList().Where(a => a.Voted).ToList();

            if (movieList.Any())
            {
                localContext.Movie.RemoveRange(movieList);
                await Context.Channel.SendMessageAsync("Movies have been cleared.");
            }
            else
            {
                await Context.Channel.SendMessageAsync("There are no movies to clear");
            }

            foreach (var user in userList)
            {
                user.Voted = false;
            }

            await localContext.SaveChangesAsync();
        }
    }
}