namespace Library.Api.Hubs;

public static class LibraryEvents
{
    public static readonly string BookCreated = "bookCreated";
    public static readonly string BookUpdated = "bookUpdated";
    public static readonly string BookDeleted = "bookDeleted";
    public static readonly string BookFavorited = "bookFavorited";
    public static readonly string BookUnfavorited = "bookUnfavorited";
    public static readonly string BookRead = "bookRead";
    public static readonly string StatsUpdated = "statsUpdated";
}


