﻿using Newtonsoft.Json;
using SlackAPI;
using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;

namespace FocusSession.Controls
{
    public class Timer
    {
        /* variables */
        private static DateTime startTime;
        private static DateTime endTime;
        private static System.Timers.Timer aTimer;
        private static System.Collections.Generic.List<Microsoft.Graph.Message> emailsReplied = new System.Collections.Generic.List<Microsoft.Graph.Message>(); // this list is simply to keep track of the already replied to emails during the session
        private static readonly string replyMessage = "Thank you for your Email. This is an automatically generated response by PersonalAnalytics. This mail-inbox is currently paused for a specific timeframe, after which your email will be received.";
        private static NotifyIcon notification; // workaround: this is to show a ballontip, if focusAssist is not set to 'alarms only', the user will see it. The icon itself will show that a focusSession is running

        public static bool openSession { get; set; } = false;   // indicate if an openSession is running
        public static bool closedSession { get; set; } = false; // indicate if a closedSession is running

        // list of potentially distracting programs that we use for flagging check
        private static String[] windowFlaggerList = new string[3] { "Zulip", "Microsoft Teams", "Mozilla Thunderbird" };

        /* getter */

        // for icon hover information
        public static TimeSpan getSessionTime()  // get the current session Time
        {
            if (openSession)
            {
                return DateTime.Now - startTime;    // return for how long the open session has been running
            }
            if (closedSession)
            {
                return endTime - startTime;         // return for how long the closed session will still be running
            }
            return TimeSpan.Zero;
        }

        /* main methods */

        // starts a session. Input: Enum if open or closed Session
        public static void StartSession(Enum.SessionEnum.Session session)
        {
            // check that there is not another session already running
            if (!openSession && !closedSession)
            {
                // set startTime
                startTime = DateTime.Now;

                if (session == Enum.SessionEnum.Session.openSession)
                {
                    // update indicator
                    openSession = true;

                    // log that the user started an openSession
                    Shared.Data.Database.GetInstance().LogInfo("StartSession : The participant started an openFocusSession at " + DateTime.Now);
                }
                // start closedSession
                else if (session == Enum.SessionEnum.Session.closedSession)
                {
                    // add the timeperiod, default is Pomodoro Timer 25 min, unless changed through the Settings
                    endTime = DateTime.Now.AddMinutes(Settings.ClosedSessionDuration);

                    // update indicator
                    closedSession = true;

                    // log that the user started a closedFocusSession
                    Shared.Data.Database.GetInstance().LogInfo("StartSession : The participant started a closedFocusSession at " + DateTime.Now + " for " + Settings.ClosedSessionDuration + " minutes.");
                }

                // since there if no officially supported API by Microsoft to check the Focus assist status, we have this little workaround
                // if Focus assist is not active / not set to 'Priority only' nor 'Alarms only', the user will actually see the message. Otherwise, it will not show up. It is viewable in the Notifications tray, but will be disposed when a session is stopped.
                // The icon at the same time serves as indicator that there is an active session running
                notification = new NotifyIcon(); // make a new instance of the object, since when stopping the session, the instance will be disposed
                notification.Visible = true;
                notification.BalloonTipTitle = "FocusSession";
                notification.BalloonTipText = "Set FocusAssist to 'Alarms only'";
                notification.Icon = SystemIcons.Information;
                notification.Text = "FocusSession: Session active";
                notification.ShowBalloonTip(40000); // attempting maximum timeout value. This is enforced and handled by the operating system, typically 30 seconds is the max

                // set the timer, which also handles session functionality. We start a timer in the openSession to make use of the session functionality
                SetTimer();
            }
            else if (openSession)
            {
                // log that the user tried to start a session
                Shared.Data.Database.GetInstance().LogInfo("StartSession : The participant tried to start a session with an active openSession already running)");
            }
            else
            {
                // log that the user tried to start a session
                Shared.Data.Database.GetInstance().LogInfo("StartSession : The participant tried to start a session with an active closedSession already running)");
            }
        }

        // Input if manually stopped or timed out
        public static void StopSession(Enum.SessionEnum.StopEvent stopEvent)
        {
            if (openSession || closedSession)
            {
                // get the current timestamp
                DateTime stopTime = DateTime.Now;

                // calculate the timespan
                TimeSpan elapsedTime = stopTime - startTime;

                // initialize endMessage to display to the participant
                StringBuilder endMessage = new StringBuilder("You did focus for " + elapsedTime.Hours + " hours and " + elapsedTime.Minutes + " Minutes. Good job :)");

                // specific to session type
                if (stopEvent == Enum.SessionEnum.StopEvent.manual)
                {
                    // log which session the user stopped
                    if (openSession)
                    {
                        Shared.Data.Database.GetInstance().LogInfo("StopSession : The participant stopped an openFocusSession at " + DateTime.Now);
                    }
                    else
                    {
                        Shared.Data.Database.GetInstance().LogInfo("StopSession : The participant stopped a closedFocusSession at " + DateTime.Now);
                    }

                    // update indicator
                    openSession = false;
                }
                else
                {
                    // log that a closedFocusSession ran out
                    Shared.Data.Database.GetInstance().LogInfo("StopSession : A closedFocusSession ran out at " + DateTime.Now);

                    // update indicator
                    closedSession = false;

                    // indicate that timer has run out in endMessage
                    endMessage.Insert(0, "FocusSession timer elapsed. ");
                }

                // store in database
                Data.Queries.SaveTime(startTime, stopTime, elapsedTime);

                // also store in log
                Shared.Data.Database.GetInstance().LogInfo("StopSession : The session had been running for " + elapsedTime);

                // stop if a timer is running
                if (aTimer != null && aTimer.Enabled)
                {
                    aTimer.Stop();
                    aTimer.Dispose();
                }

                string emailsAnswered = "During this Session, " + emailsReplied.Count + " Emails have been automatically replied.";

                // log how many emails have been answered
                Shared.Data.Database.GetInstance().LogInfo("StopSession : " + emailsAnswered);

                // add emails replied info if there were any
                if (emailsReplied.Count > 0)
                {
                    endMessage.Append(emailsAnswered);
                }

                // display a message to the user so the user gets feedback (important)
                MessageBox.Show("FocusSession stopped.");

                // workaround: calling twice because of 'splash screen dismisses dialog box' bug. More here https://stackoverflow.com/questions/576503/how-to-set-wpf-messagebox-owner-to-desktop-window-because-splashscreen-closes-mes/5328590#5328590
                MessageBox.Show(endMessage.ToString());

                // empty replied Emails list
                emailsReplied = new System.Collections.Generic.List<Microsoft.Graph.Message>();

                notification.Dispose();
            }
        }

        /* helper methods */

        private static void SetTimer()
        {
            // 10 sec interval, checking and replying to emails or ending session
            aTimer = new System.Timers.Timer(10000);

            // Hook up the Elapsed event for the timer.
            aTimer.Elapsed += OnTimedEvent;
            aTimer.AutoReset = true;
            aTimer.Enabled = true;
        }

        private static async void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            // check if this is a closed session where a timer actually runs out, and if we hit the endTime already
            if (closedSession && DateTime.Compare(DateTime.Now, endTime) > 0)
            {
                StopSession(Enum.SessionEnum.StopEvent.timed);
            }
            else
            {
                //SendSlackMessage();
                await CheckMail();
            }
        }

        private static async Task CheckMail()
        {
            // check mail and send an automatic reply if there was a new email.
            var unreadEmailsReceived = MsOfficeTracker.Helpers.Office365Api.GetInstance().GetUnreadEmailsReceived(DateTime.Now.Date);
            unreadEmailsReceived.Wait();
            foreach (Microsoft.Graph.Message email in unreadEmailsReceived.Result)
            {
                // check if this email had been received after the session started
                if (email.ReceivedDateTime.Value.LocalDateTime > startTime)
                {
                    // check if we have already replied to this email during this session
                    if (emailsReplied.Contains(email))
                    {
                        // do nothing, we already replied
                    }
                    // else reply to the email and add it to the emailsReplied List
                    else
                    {
                        //TODO add reply call to office365Api, call it here (with the email.From.EmailAddress as reply parameter?)
                        // if ReplyTo is speficied, per RFC 2822 we should send it to that address, otherwise to the from address
                        await MsOfficeTracker.Helpers.Office365Api.GetInstance().SendReplyEmail(email.Id, email.From.EmailAddress.Name, email.From.EmailAddress.Address, replyMessage);

                        //add email to list of already replied emails during this focus session
                        emailsReplied.Add(email);
                    }
                }
            }
        }

        private static void SendSlackMessage()
        {
            if (System.IO.File.Exists(Path.Combine(Shared.Settings.ExportFilePath, @"SlackConfig.json")))
            {
                // deserialized config.json to fetch tokens from class
                string allText = System.IO.File.ReadAllText(Path.Combine(Shared.Settings.ExportFilePath, @"SlackConfig.json"));
                Configuration.SlackConfig slackConfig = JsonConvert.DeserializeObject<Configuration.SlackConfig>(allText);

                // test sending message to slack via FocusSession Bot
                // does an asynchronous call mess up the messagebox flagging?
                var p = new Async();
                if (!(slackConfig.botAuthToken == null || slackConfig.botAuthToken.Equals("")))
                {
                    p.SendSlackMessageUsingAPI(slackConfig.botAuthToken).Wait();
                }
            }
        }

        // this method is called by the WindowsActivityTracker Demon, upon a foreground window/program switch, in case of an active FocusSession running
        // it checks if it is a potentially distracting program according to the list, currently printing to the Console
        public static void WindowFlagger(String currentWindowTitle)
        {
            foreach (String windowFlagger in windowFlaggerList)
                if (currentWindowTitle.Contains(windowFlagger))
                {
                    // show message box to ask if this is task-related
                    var selectedOption = MessageBox.Show("You opened a potentially distracting program during an active FocusSession. Do you want to read or reply to a message that is related to the task you are currently focussing on?", "Potentially distracting Program detected", MessageBoxButtons.YesNo);

                    // log the users answer
                    Shared.Data.Database.GetInstance().LogInfo("WindowFlagger : The participant opened " + currentWindowTitle + " and was shown the WindowFlagger Messagebox");

                    // check answer
                    // TODO store in database entry for study rather then just console-outprinting
                    if (selectedOption == DialogResult.Yes)

                    {
                        Console.WriteLine("The participant opened " + currentWindowTitle + " to read or reply to a message that is task-related");

                        // log the users answer
                        Shared.Data.Database.GetInstance().LogInfo("WindowFlagger : The participant opened " + currentWindowTitle + " to read or reply to a message that is task-related");
                    }
                    else if (selectedOption == DialogResult.No)

                    {
                        Console.WriteLine("The participant opened " + currentWindowTitle + " to read or reply to a message that is not task-related");

                        // log the users answer
                        Shared.Data.Database.GetInstance().LogInfo("WindowFlagger : The participant opened " + currentWindowTitle + " to read or reply to a message that is task-related");
                    }
                }
        }

        private class Async
        {
            public async Task SendSlackMessageUsingAPI(string token)
            {
                // instantiate a new Slack Client by provding a token
                var client = new SlackTaskClient(token);

                // send simple message to channel and wait for the call to complete
                var channel = "#general";
                var text = "hello world";
                var response = await client.PostMessageAsync(channel, text);

                // process response from API call
                if (response.ok)
                {
                    Console.WriteLine("Message sent successfully");
                }
                else
                {
                    Console.WriteLine("Message sending failed. error: " + response.error);
                }
            }
        }
    }
}