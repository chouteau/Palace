﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Palace.Updating
{
	public abstract class UpdaterBase
	{
		public abstract string CheckAndGet(string updateUri);
	}
}
