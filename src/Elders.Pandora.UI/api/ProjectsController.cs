﻿using Elders.Pandora.Box;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Web.Http;

namespace Elders.Pandora.UI.api
{
    [Authorize]
    public class ProjectsController : ApiController
    {
        static readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(ProjectsController));

        private string storageFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Elders", "Pandora");

        public Dictionary<string, List<Jar>> Get()
        {
            var projects = Directory.GetDirectories(storageFolder);

            var configurations = new Dictionary<string, List<Jar>>();

            foreach (var project in projects)
            {
                if (project != null)
                {
                    var projectName = project.Replace(storageFolder + "\\", "");

                    var configs = Directory.GetFiles(Path.Combine(storageFolder, projectName), "*.json", SearchOption.AllDirectories);

                    var jars = new List<Jar>();

                    foreach (var jar in configs)
                    {
                        Jar jarObject = null;

                        try
                        {
                            jarObject = JsonConvert.DeserializeObject<Jar>(File.ReadAllText(jar));
                        }
                        catch (Exception ex)
                        {
                            log.Error(ex);

                            continue;
                        }

                        jars.Add(jarObject);
                    }

                    configurations.Add(projectName, jars);
                }
            }

            return configurations;
        }

        public void Post(string projectName, string gitUrl)
        {
            var workingDir = Path.Combine(storageFolder, projectName);

            var project = Directory.Exists(workingDir);

            if (!project)
            {
                Directory.CreateDirectory(workingDir);
                try
                {
                    Git.Clone(gitUrl, workingDir);

                    string configPath = Path.Combine(workingDir, projectName + ".config");

                    File.WriteAllText(configPath, gitUrl);

                    var nameClaim = ClaimsPrincipal.Current.Identities.First().Claims.SingleOrDefault(x => x.Type == "name");
                    var username = nameClaim != null ? nameClaim.Value : "no name claim";
                    var emailClaim = ClaimsPrincipal.Current.Identities.First().Claims.SingleOrDefault(x => x.Type == "email");
                    var email = emailClaim != null ? emailClaim.Value : "no email claim";
                    var message = "Added project configuration file.";

                    var git = new Git(workingDir);
                    git.Stage(new List<string>() { configPath });
                    git.Commit(message, username, email);
                    git.Push();
                }
                catch (Exception ex)
                {
                    log.Fatal(ex);
                    throw ex;
                }
            }
        }

        public void Delete(string projectName)
        {
            var workingDir = Path.Combine(storageFolder, projectName);

            var project = Directory.Exists(workingDir);

            if (project)
            {
                Directory.Delete(workingDir);
            }
        }
    }
}