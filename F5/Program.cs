using Microsoft.EntityFrameworkCore;

List<Request> requests = new List<Request>()
{
    new Request(1,DateTime.Parse("2023-06-06"),"Computer","DEXP Aquilion O286","Perestal Rabotat","Sorokin dmirii ilich","89219567841","v processe remonta") { Master = "Ilin Aleksandr Andreevich"},
};


//YDALIT POSLE ODNOGO RAZA
foreach (var request in requests)
{
    using Repository repo = new Repository();
    repo.Add(request);
    repo.SaveChanges();
}

var builder = WebApplication.CreateBuilder();
builder.Services.AddCors();
var app = builder.Build();
app.UseCors(param => param.AllowAnyHeader().AllowAnyOrigin().AllowAnyMethod());

app.MapGet("/requests", () =>
{
    using Repository repo = new Repository();
    return repo.ReadAll();
});
app.MapGet("/requests/{id}", (int id) =>
{
    using Repository repo = new Repository();
    return repo.Read(id);
});
app.MapPost("/requests", (CreateRequestDTO dto) =>
{
    using Repository repo = new Repository();
    Request order = new Request(dto.Id, DateTime.Parse(dto.StartDate), dto.OrgTechType, dto.OrgTechModel, dto.ProblemDescription, dto.ClientFIO, dto.ClientNumber, dto.RequestStatus);
    repo.Add(order);
    repo.SaveChanges();
});
app.MapPut("/requests/{id}", (UpdateRequestDTO dto, int id) =>
{
    using Repository repo = new Repository();
    repo.Update(dto, id);
    repo.SaveChanges();
});

app.MapGet("/statistics", () =>
{
    using Repository repo = new Repository();
    var competeCount = repo.GetCompleteCount();
    var averageTime = repo.GetAverageTime();
    var stat = repo.GetStatistics();
    StatisticDTO statistic = new StatisticDTO(competeCount, averageTime, stat);
    return statistic;
});
app.Run();

class Request
{
    public Request(int id, DateTime startDate, string orgTechType, string orgTechModel, string problemDescription, string clientFIO, string clientNumber, string requestStatus)
    {
        Id = id;
        StartDate = startDate;
        OrgTechType = orgTechType;
        OrgTechModel = orgTechModel;
        ProblemDescription = problemDescription;
        ClientFIO = clientFIO;
        ClientNumber = clientNumber;
        RequestStatus = requestStatus;
    }

    public int Id { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? CompliteDate { get; set; }
    public string OrgTechType { get; set; }
    public string OrgTechModel { get; set; }
    public string ProblemDescription { get; set; }
    public string ClientFIO { get; set; }
    public string ClientNumber { get; set; }
    private string requestStatus;
    public string RequestStatus {
        get => requestStatus;
        set
        {
            if (value == "gotova k vidacha")
                CompliteDate = DateTime.Now;
            requestStatus = value;
        }
    }
    public string? Master { get; set; }
    public string? Comment { get; set; }
    public string? RepairParts { get; set; }

}
class CreateRequestDTO
{
    public int Id { get; set; }
    public string StartDate { get; set; }
    public string OrgTechType { get; set; }
    public string OrgTechModel { get; set; }
    public string ProblemDescription { get; set; }
    public string ClientFIO { get; set; }
    public string ClientNumber { get; set; }
    public string RequestStatus { get; set; }
}
class UpdateRequestDTO
{
    public string Master { get; set; }
    public string ProblemDescription { get; set; }
    public string RequestStatus { get; set; }
    public string Comment { get; set; }
    public string RepairParts { get; set; }
}
record class StatisticDTO(
    int CompleteCount,
    double AverageTime,
    Dictionary<string, int> Stat);

class Repository : DbContext
{
    private DbSet<Request> Requests { get; set; }
    public Repository()
    {
        Requests = Set<Request>();
        Database.EnsureCreated();
    }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=requests.db");
    }


    public void Add(Request request)
    {
        Requests.Add(request);
    }
    public Request Read(int id)
    {
        return Requests.Find(id);
    }
    public List<Request> ReadAll()
    {
        return Requests.ToList();
    }
    public void Update(UpdateRequestDTO dto, int id)
    {
        Request request = Read(id);
        if (dto.Master != "")
            request.Master = dto.Master;
        if (dto.ProblemDescription != "")
            request.ProblemDescription = dto.ProblemDescription;
        if (dto.RequestStatus != request.RequestStatus)
            request.RequestStatus = dto.RequestStatus;
        if (dto.RepairParts != "")
            request.RepairParts = dto.RepairParts;
        if (dto.Comment != "")
            request.Comment = dto.Comment;
    }




    public int GetCompleteCount()
    {
        return Requests.Count(x => x.RequestStatus == "gotova k vidacha");
    }
    public double GetAverageTime()
    {
        List<Request> completeRequests = Requests.ToList().FindAll(x => x.RequestStatus == "gotova k vidacha");
        if (completeRequests.Count == 0)
            return 0;
        double allTime = 0;
        foreach (Request request in completeRequests)
            allTime += (request.CompliteDate - request.StartDate).Value.Hours;
        return allTime / completeRequests.Count;
    }
    public Dictionary<string, int> GetStatistics()
    {
        Dictionary<string, int> result = new Dictionary<string, int>();
        foreach (Request request in Requests.ToList())
        {
            if (result.ContainsKey(request.ProblemDescription))
                result[request.ProblemDescription]++;
            else
                result[request.ProblemDescription] = 1;
        }
        return result;
    }
}