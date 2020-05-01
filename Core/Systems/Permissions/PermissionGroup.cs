using System;
using System.Collections.Generic;


namespace MopBot.Core.Systems.Permissions
{
	public class PermissionGroup : ICloneable
	{
		public string name;
		public Dictionary<string,bool?> permissions;

		public bool? this[string permission] {
			get => permissions.TryGetValue(permission,out var result) ? result : null;
			set => permissions[permission] = value;
		}

		private PermissionGroup() {}

		public PermissionGroup(string name)
		{
			this.name = name;
			permissions = new Dictionary<string,bool?>();
		}

		public PermissionGroup Clone()
		{
			var result = new PermissionGroup {
				name = (string)name.Clone(),
				permissions = new Dictionary<string,bool?>()
			};

			foreach(var pair in permissions) {
				result.permissions.Add(pair.Key,pair.Value);
			}

			return result;
		}

		object ICloneable.Clone() => Clone();
	}
}