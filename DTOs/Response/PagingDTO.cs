

namespace WebShoppingAPI.DTOs.Response;

public class PagingDTO<T> //T คือ Type อะไรก็ได้ Any นั่นหละ
{
    public int TotalRecords { get; set; }
    public List<T> Items { get; set; } = new List<T>();
}