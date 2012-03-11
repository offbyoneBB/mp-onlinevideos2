using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Configuration;
using System.Configuration.Provider;
using OnlineVideos.WebService.Database;

namespace OnlineVideos.WebService
{
    public class OVRoleProvider : RoleProvider 
    {
        public enum Roles { admin };

        public override string ApplicationName
        {
            get { return "OnlineVideos"; }
            set { }
        }

        public override void AddUsersToRoles(string[] usernames, string[] roleNames)
        {
            throw new NotImplementedException();
        }

        public override void CreateRole(string roleName)
        {
            throw new NotImplementedException();
        }

        public override bool DeleteRole(string roleName, bool throwOnPopulatedRole)
        {
            throw new NotImplementedException();
        }

        public override string[] FindUsersInRole(string roleName, string usernameToMatch)
        {
            throw new NotImplementedException();
        }

        public override string[] GetAllRoles()
        {
            throw new NotImplementedException();
        }

        public override string[] GetRolesForUser(string username)
        {
            if (!string.IsNullOrEmpty(username) && IsUserInRole(username, Roles.admin.ToString()))
            {
                return new string[] { Roles.admin.ToString() };
            }
            return new string[0];
        }

        public override string[] GetUsersInRole(string roleName)
        {
            throw new NotImplementedException();
        }

        public override bool IsUserInRole(string username, string roleName)
        {
            if (roleName == Roles.admin.ToString())
            {
                using (OnlineVideosDataContext dc = new OnlineVideosDataContext())
                {
                    if (!dc.DatabaseExists()) { dc.CreateDatabase(); dc.SubmitChanges(); }
                    return dc.User.Any(u => u.Email == username && u.IsAdmin);
                }
            }
            return false;
        }

        public override void RemoveUsersFromRoles(string[] usernames, string[] roleNames)
        {
            throw new NotImplementedException();
        }

        public override bool RoleExists(string roleName)
        {
            throw new NotImplementedException();
        }
    }
}