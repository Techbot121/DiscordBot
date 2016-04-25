using Discord;
using Discord.Modules;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace DiscordBot.Modules.Sed
{
    internal class SedModule : IModule
    {
        private ModuleManager _manager;
        private DiscordClient _client;

        /// <summary>
        /// what the fuck is this even
        /// </summary>
        /// <param name="manager"></param>

        void IModule.Install(ModuleManager manager)
        {
            _manager = manager;
            _client = manager.Client;
            Dictionary<Message, User> BackLog = new Dictionary<Message, User>();

            _client.MessageReceived += async (s, e) =>
           {
               try
               { 
                   if (Regex.IsMatch(e.Message.Text, @"\bs/.*/.*/"))
                   {
                       string om = e.Message.Text;
                       /// ???????????
                       string what = Regex.Match(om, @"\bs/(.*)/.*/").Groups[1].Value;
                       string repl = Regex.Match(om, @"\bs/.*/(.*)/").Groups[1].Value;
                       /// ???????????
                       var result = BackLog.OrderBy(x => x.Key.Timestamp).Where(x => x.Key.Text.Split(null).Contains(what));

                       if (result.Any())
                       {
                           var msgusr = result.FirstOrDefault().Key.User;
                           string[] ssorg = result.FirstOrDefault().Key.Text.Split(null).Select(x => x.Replace(what, repl)).ToArray();
                           string replacement = string.Join(" ", ssorg);

                           StringBuilder sb = new StringBuilder();

                           if (e.Message.User == msgusr)
                           {
                               sb.Clear();
                               sb.Append($"{Format.Bold(e.Message.User.Name)} meant to say:\n\n{replacement.Trim()}");
                           }
                           else
                           {
                               sb.Clear();
                               sb.Append($"{Format.Bold(e.Message.User.Name)} thinks {Format.Bold(msgusr.Name)} meant to say:\n\n{replacement.Trim()}");
                           }
                           await e.Channel.SendMessage(sb.ToString());
                       }
                       else
                           return;
                   }
                   else
                   {
                       if (BackLog.Count >= 50)
                       {
                           BackLog.Remove(BackLog.Keys.OrderBy(x => x.Timestamp).FirstOrDefault());
                       }
                       BackLog.Add(e.Message, e.User);
                   }
               }
               catch { }
           };
        }
    }
}