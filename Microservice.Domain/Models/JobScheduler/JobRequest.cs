using MessagePack;
using Microservice.Domain.Base.Enums;


namespace Microservice.Domain;
[MessagePackObject]
public class JobRequest
{
    [Key(0)]
    public HarvesterType HarvesterType { get; set; }
}

