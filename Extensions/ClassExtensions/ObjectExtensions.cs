﻿using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SammBotNET.Extensions
{
	public static class ObjectExtensions
	{
		public static string ToQueryString(this object obj)
		{
			IEnumerable<string> properties = from p in obj.GetType().GetProperties()
											 where p.GetValue(obj, null) != null
											 select p.Name + "=" + HttpUtility.UrlEncode(p.GetValue(obj, null).ToString());

			return string.Join("&", properties.ToArray());
		}
	}
}
