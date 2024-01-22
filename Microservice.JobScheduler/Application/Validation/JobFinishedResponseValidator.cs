using FluentValidation;
using Microservice.Domain.Models.JobModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microservice.JobScheduler.Application.Validation;

internal class JobFinishedResponseValidator : AbstractValidator<JobFinishedResponse>
{
}
