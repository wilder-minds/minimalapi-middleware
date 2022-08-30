namespace BechdelDataServer.Models;

public record FilmResult(int TotalCount, 
    IEnumerable<Film>? Results)
{
  public static FilmResult Default = new FilmResult(0, null);
}
