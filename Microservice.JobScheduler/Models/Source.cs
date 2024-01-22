using Microservice.Domain.Base.Enums;
using Microservice.JobScheduler.Models;
using System.ComponentModel.DataAnnotations;


namespace Microservice.Domain.Models.Scheduler;

public class Source : BaseEntity
{
    [Display(Name = "Address", Description = "Used like a address/path to the choosen source")]
    public string Address { get; init; } = string.Empty;

    [Display(Name = "ObjectSid", Description = "User ObjectSid to know who created the source")]
    public string ObjectSid { get; init; } = string.Empty;

    [Display(Name = "Source Type", Description = "To check what harvester type the source is")]
    public HarvesterType Type { get; set; }

    [Display(Name = "itemsCount", Description = "Number of items found in the source")]
    public int itemsCount { get; set; }

    [Display(Name = "Jobs", Description = "See which jobs that use this source")]
    public IEnumerable<Job>? Jobs { get; set; }

}
