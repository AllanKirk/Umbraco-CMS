﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Umbraco.Core.PropertyEditors
{
    public interface IDataTypeConfiguration
    {
        IDictionary<string, object> ToDictionary();
    }
}
