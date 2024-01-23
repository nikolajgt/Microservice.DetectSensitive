using Microservice.Domain.Base.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microservice.Domain.Enums;

[Flags]
public enum JobType
{
    ExchangeGenerateJobs = 1 << 0,
    ExchangeScanItems = 1 << 1,
    ExchangeProcessActions = 1 << 2,
    FileSystemScanDirectory = 1 << 3,
    FileSystemProcessActions = 1 << 4,
}

public static class JobTypeExtensions
{
    public static HarvesterType IsExchangeOrFileSystem(this JobType jobType)
    {
        JobType exchangeFlags = JobType.ExchangeGenerateJobs | JobType.ExchangeScanItems | JobType.ExchangeProcessActions;
        //JobType fileSystemFlags = JobType.FileSystemScanDirectory | JobType.FileSystemProcessActions;

        if ((jobType & exchangeFlags) != 0)
            return HarvesterType.Exchange;

        else
            return HarvesterType.FileSystem;
    }

    public static string GetQueueName(this JobType jobType)
    {
        JobType exchangeFlags = JobType.ExchangeGenerateJobs | JobType.ExchangeScanItems | JobType.ExchangeProcessActions;
        //JobType fileSystemFlags = JobType.FileSystemScanDirectory | JobType.FileSystemProcessActions;

        if ((jobType & exchangeFlags) != 0)
            return "Exchange";

        else
            return "FileSystem";
    }
}