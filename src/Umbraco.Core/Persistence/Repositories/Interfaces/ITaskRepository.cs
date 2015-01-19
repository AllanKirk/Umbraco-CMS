using System;
using System.Collections.Generic;
using Umbraco.Core.Models;

namespace Umbraco.Core.Persistence.Repositories
{
    public interface ITaskRepository : IRepositoryQueryable<int, Task>
    {
        //IEnumerable<Task> GetTasks(Guid? itemId = null, int? assignedUser = null, int? ownerUser = null, string taskTypeAlias = null, bool includeClosed = false);
        IEnumerable<Task> GetTasks(int? itemId = null, int? assignedUser = null, int? ownerUser = null, string taskTypeAlias = null, bool includeClosed = false);

        //IEnumerable<Task> GetTasksForItem(int id);
        //IEnumerable<Task> GetTasksForItem(Guid uniqueId);
        //IEnumerable<Task> GetTasksByType(int typeId);
        //IEnumerable<Task> GetTasksByType(string typeAlias);

    }
}