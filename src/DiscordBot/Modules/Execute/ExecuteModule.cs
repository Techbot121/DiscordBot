/*using Discord;
using Discord.Commands;
using Discord.Commands.Permissions.Levels;
using Discord.Modules;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.Dnx.Compilation;
using Microsoft.Dnx.Compilation.CSharp;
using Microsoft.Extensions.PlatformAbstractions;
using System;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;

namespace DiscordBot.Modules.Execute
{
    public class ScriptGlobals
    {
        public CommandEventArgs e { get; internal set; }
        public DiscordClient client { get; internal set; }
    }

    /// <summary> Allows the execution of scripts from within Discord. Be very careful with the permissions of this module - allowing remote execution for anyone but the bot owner is generally a bad idea.  </summary>
    internal class ExecuteModule : IModule
	{

        private readonly IApplicationEnvironment _dnxEnvironment;
        private readonly ILibraryExporter _libExporter;
        private ModuleManager _manager;
		private DiscordClient _client;

        public ExecuteModule(IApplicationEnvironment env, ILibraryExporter libEx)
        {
            _dnxEnvironment = env;
            _libExporter = libEx;
        }

        void IModule.Install(ModuleManager manager)
		{
			_manager = manager;
			_client = manager.Client;

			var references = _libExporter.GetAllExports(_dnxEnvironment.ApplicationName).MetadataReferences;
			var options = ScriptOptions.Default
				.AddReferences(references.Select(x => ConvertMetadataReference(x)))
				.AddImports("System.Collections.Generic", "System.Linq");

            manager.CreateCommands("", group =>
			{
				group.MinPermissions((int)PermissionLevel.BotOwner);

				group.CreateCommand("eval")
					.Description("Runs a C# sniplet and returns the result")
					.Parameter("code", ParameterType.Unparsed)
					.Do(async e =>
					{
						var globals = new ScriptGlobals { e = e, client = _client };
						var text = e.Args[0].Trim('`'); //Remove code block tags

						try
						{
							var script = CSharpScript.Create(text, options, typeof(ScriptGlobals));
							var scriptState = await script.RunAsync(globals);
							var returnValue = scriptState.ReturnValue;
							if (returnValue != null)
								await _client.Reply(e, returnValue.ToString());
						}
						catch (Exception ex)
						{
							await _client.ReplyError(e, ex);
						}
					});
				group.CreateCommand("exec")
					.Alias("run")
					.Description("Runs a C# sniplet")
					.Parameter("code", ParameterType.Unparsed)
					.Do(async e =>
					{
						var globals = new ScriptGlobals { e = e, client = _client };
						var text = e.Args[0].Trim('`'); //Remove code block tags

						try
						{
							var script = CSharpScript.Create(text, options, typeof(ScriptGlobals));
							await script.RunAsync(globals);
						}
						catch (Exception ex)
						{
							await _client.ReplyError(e, ex);
						}
					});
			});
		}

		//Source: https://github.com/aspnet/Mvc/blob/dev/src/Microsoft.AspNet.Mvc.Razor/Compilation/RoslynCompilationService.cs
		private MetadataReference ConvertMetadataReference(IMetadataReference metadataReference)
		{
			var roslynReference = metadataReference as IRoslynMetadataReference;
			if (roslynReference != null)
				return roslynReference.MetadataReference;

			var embeddedReference = metadataReference as IMetadataEmbeddedReference;
			if (embeddedReference != null)
				return MetadataReference.CreateFromImage(embeddedReference.Contents);

			var fileMetadataReference = metadataReference as IMetadataFileReference;
			if (fileMetadataReference != null)
			{
				using (var stream = File.OpenRead(fileMetadataReference.Path))
				{
					var moduleMetadata = ModuleMetadata.CreateFromStream(stream, PEStreamOptions.PrefetchMetadata);
					return AssemblyMetadata.Create(moduleMetadata).GetReference(filePath: fileMetadataReference.Path);
				}
			}
            
			var projectReference = metadataReference as IMetadataProjectReference;
			if (projectReference != null)
			{
				using (var ms = new MemoryStream())
				{
					projectReference.EmitReferenceAssembly(ms);
					return MetadataReference.CreateFromImage(ms.ToArray());
				}
			}

            throw new NotSupportedException();
		}
	}
}
*/