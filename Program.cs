using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<CareerStore>();
var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/jobs", (CareerStore store) => Results.Ok(store.GetJobs()));
app.MapPost("/api/jobs", async (NewJob input, CareerStore store) =>
{
    if (string.IsNullOrWhiteSpace(input.Title) || string.IsNullOrWhiteSpace(input.Company))
        return Results.BadRequest(new { message = "Job title and company are required." });

    var job = new Job
    {
        Id = Guid.NewGuid().ToString(),
        Title = input.Title.Trim(),
        Company = input.Company.Trim(),
        Location = input.Location?.Trim() ?? "",
        Type = input.Type?.Trim() ?? "Not specified",
        Source = string.IsNullOrWhiteSpace(input.Source) ? "Added manually" : input.Source.Trim(),
        Deadline = input.Deadline,
        Url = input.Url?.Trim() ?? "",
        Description = input.Description?.Trim() ?? "",
        Tags = [],
        Saved = true,
        Demo = false
    };
    await store.AddJob(job);
    return Results.Created($"/api/jobs/{job.Id}", job);
});
app.MapPatch("/api/jobs/{id}", async (string id, JobUpdate update, CareerStore store) =>
    await store.SetJobSaved(id, update.Saved) is { } job ? Results.Ok(job) : Results.NotFound());

app.MapGet("/api/applications", (CareerStore store) => Results.Ok(store.GetApplications()));
app.MapPost("/api/applications", async (NewApplication input, CareerStore store) =>
{
    if (string.IsNullOrWhiteSpace(input.Title) || string.IsNullOrWhiteSpace(input.Company))
        return Results.BadRequest(new { message = "Job title and company are required." });

    var application = new Application
    {
        Id = Guid.NewGuid().ToString(),
        Title = input.Title.Trim(),
        Company = input.Company.Trim(),
        Status = string.IsNullOrWhiteSpace(input.Status) ? "Preparing" : input.Status.Trim(),
        Date = input.Date,
        Url = input.Url?.Trim() ?? "",
        Notes = input.Notes?.Trim() ?? "",
        Follow = "",
        Updated = DateOnly.FromDateTime(DateTime.Today).ToString("yyyy-MM-dd")
    };
    await store.AddApplication(application);
    return Results.Created($"/api/applications/{application.Id}", application);
});
app.MapPatch("/api/applications/{id}", async (string id, ApplicationUpdate update, CareerStore store) =>
    await store.UpdateApplication(id, update) is { } record ? Results.Ok(record) : Results.NotFound());
app.MapDelete("/api/applications/{id}", async (string id, CareerStore store) =>
    await store.DeleteApplication(id) ? Results.NoContent() : Results.NotFound());

app.MapFallbackToFile("index.html");
app.Run();

public sealed class CareerStore
{
    private readonly object gate = new();
    private readonly SemaphoreSlim writeGate = new(1, 1);
    private readonly string filePath;
    private List<Job> jobs;
    private List<Application> applications;

    public CareerStore(IWebHostEnvironment environment)
    {
        var dataDirectory = Path.Combine(environment.ContentRootPath, "App_Data");
        Directory.CreateDirectory(dataDirectory);
        filePath = Path.Combine(dataDirectory, "career-data.json");
        try
        {
            var saved = File.Exists(filePath)
                ? JsonSerializer.Deserialize<CareerData>(File.ReadAllText(filePath), JsonOptions.Default)
                : null;
            jobs = saved?.Jobs ?? SeedJobs();
            applications = saved?.Applications ?? [];
        }
        catch (JsonException)
        {
            jobs = SeedJobs();
            applications = [];
        }
    }

    public List<Job> GetJobs() { lock (gate) return jobs.Select(x => x.Copy()).ToList(); }
    public List<Application> GetApplications() { lock (gate) return applications.Select(x => x.Copy()).ToList(); }

    public Task AddJob(Job job)
    {
        lock (gate) jobs.Insert(0, job);
        return Persist();
    }

    public async Task<Job?> SetJobSaved(string id, bool? saved)
    {
        Job? result;
        lock (gate)
        {
            var job = jobs.FirstOrDefault(x => x.Id == id);
            if (job is null) return null;
            if (saved.HasValue) job.Saved = saved.Value;
            if (update.Title is not null) job.Title = update.Title;
            if (update.Company is not null) job.Company = update.Company;
            if (update.Location is not null) job.Location = update.Location;
            if (update.Type is not null) job.Type = update.Type;
            if (update.Source is not null) job.Source = update.Source;
            if (update.Deadline is not null) job.Deadline = update.Deadline;
            if (update.Url is not null) job.Url = update.Url;
            if (update.Description is not null) job.Description = update.Description;
            if (update.Tags is not null) job.Tags = update.Tags;
            result = job.Copy();
        }
        await Persist();
        return result;
    }

    public Task AddApplication(Application application)
    {
        lock (gate) applications.Insert(0, application);
        return Persist();
    }

    public async Task<Application?> UpdateApplication(string id, ApplicationUpdate update)
    {
        Application? result;
        lock (gate)
        {
            var record = applications.FirstOrDefault(x => x.Id == id);
            if (record is null) return null;
            if (update.Status is not null) record.Status = update.Status;
            if (update.Follow is not null) record.Follow = update.Follow;
            if (update.Notes is not null) record.Notes = update.Notes;
            record.Updated = DateOnly.FromDateTime(DateTime.Today).ToString("yyyy-MM-dd");
            result = record.Copy();
        }
        await Persist();
        return result;
    }

    public async Task<bool> DeleteApplication(string id)
    {
        bool deleted;
        lock (gate) deleted = applications.RemoveAll(x => x.Id == id) > 0;
        if (deleted) await Persist();
        return deleted;
    }

    private async Task Persist()
    {
        await writeGate.WaitAsync();
        try
        {
            CareerData snapshot;
            lock (gate) snapshot = new CareerData(jobs.Select(x => x.Copy()).ToList(), applications.Select(x => x.Copy()).ToList());
            var json = JsonSerializer.Serialize(snapshot, JsonOptions.Default);
            var tempPath = filePath + ".tmp";
            await File.WriteAllTextAsync(tempPath, json);
            File.Move(tempPath, filePath, overwrite: true);
        }
        finally
        {
            writeGate.Release();
        }
    }

    private static List<Job> SeedJobs() =>
    [
        new() { Id = "demo-1", Title = "Library Services Assistant", Company = "Northstar University Library (Fictional)", Location = "Los Angeles, CA", Type = "On-site", Source = "Sample data", Description = "Welcome library visitors, help with circulation and study space requests, and keep service information organized. Fictional sample listing only; not a real opening.", Tags = ["Customer service", "Organization", "Communication"], Demo = true },
        new() { Id = "demo-2", Title = "Student Programs Assistant", Company = "Bruin Student Success Center (Fictional)", Location = "Los Angeles, CA", Type = "Hybrid", Source = "Sample data", Description = "Help coordinate student workshops, respond to routine questions, and maintain program schedules and materials. Fictional sample listing only; not a real opening.", Tags = ["Student services", "Event coordination", "Communication"], Demo = true },
        new() { Id = "demo-3", Title = "Undergraduate Research Data Assistant", Company = "Pacific Research Institute (Fictional)", Location = "Remote · California", Type = "Remote", Source = "Sample data", Description = "Organize research records, check spreadsheet entries for accuracy, and prepare clear documentation for a research team. Fictional sample listing only; not a real opening.", Tags = ["Research", "Data entry", "Attention to detail"], Demo = true }
    ];
}

public sealed record CareerData(List<Job> Jobs, List<Application> Applications);
public sealed record NewJob(string Title, string Company, string? Location, string? Type, string? Source, string? Deadline, string? Url, string? Description);
public sealed record JobUpdate(bool? Saved, string? Title, string? Company, string? Location, string? Type, string? Source, string? Deadline, string? Url, string? Description, List<string>? Tags);
public sealed record NewApplication(string Title, string Company, string? Status, string? Date, string? Url, string? Notes);
public sealed record ApplicationUpdate(string? Status, string? Follow, string? Notes);

public sealed class Job
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Company { get; set; } = "";
    public string Location { get; set; } = "";
    public string Type { get; set; } = "";
    public string Source { get; set; } = "";
    public string? Deadline { get; set; }
    public string Url { get; set; } = "";
    public string Description { get; set; } = "";
    public List<string> Tags { get; set; } = [];
    public bool Saved { get; set; }
    public bool Demo { get; set; }
    public Job Copy()
    {
        var copy = (Job)MemberwiseClone();
        copy.Tags = Tags.ToList();
        return copy;
    }
}

public sealed class Application
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Company { get; set; } = "";
    public string Status { get; set; } = "Preparing";
    public string? Date { get; set; }
    public string Url { get; set; } = "";
    public string Notes { get; set; } = "";
    public string Follow { get; set; } = "";
    public string Updated { get; set; } = "";
    public Application Copy() => (Application)MemberwiseClone();
}

public static class JsonOptions
{
    public static readonly JsonSerializerOptions Default = new(JsonSerializerDefaults.Web) { WriteIndented = true };
}
