﻿using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using ServiceStack.ServiceHost;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MediaBrowser.Api.UserLibrary
{
    [Route("/GameGenres", "GET")]
    [Api(Description = "Gets all Game genres from a given item, folder, or the entire library")]
    public class GetGameGenres : GetItemsByName
    {
        public GetGameGenres()
        {
            MediaTypes = MediaType.Game;
        }
    }

    [Route("/GameGenres/{Name}/Counts", "GET")]
    [Api(Description = "Gets item counts of library items that a genre appears in")]
    public class GetGameGenreItemCounts : IReturn<ItemByNameCounts>
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        [ApiMember(Name = "UserId", Description = "User Id", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public Guid? UserId { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        [ApiMember(Name = "Name", Description = "The genre name", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Name { get; set; }
    }

    [Route("/GameGenres/{Name}", "GET")]
    [Api(Description = "Gets a Game genre, by name")]
    public class GetGameGenre : IReturn<BaseItemDto>
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        [ApiMember(Name = "Name", Description = "The genre name", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        [ApiMember(Name = "UserId", Description = "Optional. Filter by user id, and attach user data", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public Guid? UserId { get; set; }
    }

    public class GameGenresService : BaseItemsByNameService<GameGenre>
    {
        public GameGenresService(IUserManager userManager, ILibraryManager libraryManager, IUserDataRepository userDataRepository, IItemRepository itemRepo)
            : base(userManager, libraryManager, userDataRepository, itemRepo)
        {
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetGameGenre request)
        {
            var result = GetItem(request).Result;

            return ToOptimizedResult(result);
        }

        /// <summary>
        /// Gets the item.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>Task{BaseItemDto}.</returns>
        private async Task<BaseItemDto> GetItem(GetGameGenre request)
        {
            var item = await GetGameGenre(request.Name, LibraryManager).ConfigureAwait(false);

            // Get everything
            var fields = Enum.GetNames(typeof(ItemFields)).Select(i => (ItemFields)Enum.Parse(typeof(ItemFields), i, true));

            var builder = new DtoBuilder(Logger, LibraryManager, UserDataRepository, ItemRepository);

            if (request.UserId.HasValue)
            {
                var user = UserManager.GetUserById(request.UserId.Value);

                return await builder.GetBaseItemDto(item, fields.ToList(), user).ConfigureAwait(false);
            }

            return await builder.GetBaseItemDto(item, fields.ToList()).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetGameGenres request)
        {
            var result = GetResult(request).Result;

            return ToOptimizedResult(result);
        }

        /// <summary>
        /// Gets all items.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="items">The items.</param>
        /// <returns>IEnumerable{Tuple{System.StringFunc{System.Int32}}}.</returns>
        protected override IEnumerable<IbnStub<GameGenre>> GetAllItems(GetItemsByName request, IEnumerable<BaseItem> items)
        {
            var itemsList = items.Where(i => i.Genres != null).ToList();

            return itemsList
                .SelectMany(i => i.Genres)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(name => new IbnStub<GameGenre>(name, () => itemsList.Where(i => i.Genres.Contains(name, StringComparer.OrdinalIgnoreCase)), GetEntity));
        }

        /// <summary>
        /// Gets the entity.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>Task{Genre}.</returns>
        protected Task<GameGenre> GetEntity(string name)
        {
            return LibraryManager.GetGameGenre(name);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetGameGenreItemCounts request)
        {
            var name = DeSlugGameGenreName(request.Name, LibraryManager);

            var items = GetItems(request.UserId).Where(i => i.Genres != null && i.Genres.Contains(name, StringComparer.OrdinalIgnoreCase)).ToList();

            var counts = new ItemByNameCounts
            {
                TotalCount = items.Count,

                GameCount = items.OfType<Game>().Count()
            };

            return ToOptimizedResult(counts);
        }
    }
}
