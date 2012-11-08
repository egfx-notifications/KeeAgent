﻿using KeeAgent.UI;
using System;
using KeePassLib;
using KeePass.Plugins;
using KeePassPluginTestUtil;
using KeeAgent;
using System.Windows.Forms;
using KeeAgentTestProject.Properties;
using KeePassLib.Security;
using System.Security.Cryptography;
using System.Threading;

namespace KeeAgentDebug
{


  public static class Program
  {

    public static void Main()
    {
      using (KeePassAppDomain appDomain = new KeePassAppDomain()) {
        bool success = appDomain.StartKeePass(true, true, 2, true);
        if (!success) {
          MessageBox.Show("KeePass failed to start.");
          return;
        }

        appDomain.DoCallBack(delegate()
        {

          Plugin keeAgent = new KeeAgentExt();
          IPluginHost pluginHost = KeePass.Program.MainForm.PluginHost;

          PwEntry withPassEntry = new PwEntry(true, true);
          withPassEntry.Strings.Set(PwDefs.TitleField, new ProtectedString(true, "with-passphrase"));
          withPassEntry.Binaries.Set("withPass.ppk", new ProtectedBinary(true, Resources.withPassphrase_ppk));
          withPassEntry.Strings.Set(PwDefs.PasswordField, new ProtectedString(true, "KeeAgent"));

          PwEntry withBadPassEntry = new PwEntry(true, true);
          withBadPassEntry.Strings.Set(PwDefs.TitleField, new ProtectedString(true, "with-bad-passphrase"));
          withBadPassEntry.Binaries.Set("withBadPass.ppk", new ProtectedBinary(true, Resources.withPassphrase_ppk));
          withBadPassEntry.Strings.Set(PwDefs.PasswordField, new ProtectedString(true, "BadPass"));

          PwEntry withoutPassEntry = new PwEntry(true, true);
          withoutPassEntry.Strings.Set(PwDefs.TitleField, new ProtectedString(true, "without-passphrase"));
          withoutPassEntry.Binaries.Set("withoutPass.ppk", new ProtectedBinary(true, Resources.withoutPassphrase_ppk));

          PwEntry dsaPassEntry = new PwEntry(true, true);
          dsaPassEntry.Strings.Set(PwDefs.TitleField, new ProtectedString(true, "dsa-with-passphrase"));
          dsaPassEntry.Binaries.Set("dsaWithPass.ppk", new ProtectedBinary(true, Resources.dsa_ppk));
          dsaPassEntry.Strings.Set(PwDefs.PasswordField, new ProtectedString(true, "KeeAgent"));

          PwEntry nonStandardLengthPassEntry = new PwEntry(true, true);
          nonStandardLengthPassEntry.Strings.Set(PwDefs.TitleField, new ProtectedString(true, "4095-bits"));
          nonStandardLengthPassEntry.Binaries.Set("4095-bits.ppk", new ProtectedBinary(true, Resources._4095_bits_ppk));
          nonStandardLengthPassEntry.Strings.Set(PwDefs.PasswordField, new ProtectedString(true, "KeeAgent"));

          PwEntry nonAsciiPassEntry = new PwEntry(true, true);
          nonAsciiPassEntry.Strings.Set(PwDefs.TitleField, new ProtectedString(true, "non-ascii passphrase"));
          nonAsciiPassEntry.Binaries.Set("non-ascii-passphrase.ppk", new ProtectedBinary(true, Resources.non_ascii_passphrase_ppk));
          nonAsciiPassEntry.Strings.Set(PwDefs.PasswordField, new ProtectedString(true, "Ŧéşť"));

          PwGroup rsaGroup = new PwGroup(true, true, "RSA", PwIcon.Key);
          rsaGroup.AddEntry(withPassEntry, true);
          //rsaGroup.AddEntry(withBadPassEntry, true);
          rsaGroup.AddEntry(withoutPassEntry, true);
          rsaGroup.AddEntry(nonAsciiPassEntry, true);

          PwGroup dsaGroup = new PwGroup(true, true, "DSA", PwIcon.Key);
          dsaGroup.AddEntry(dsaPassEntry, true);
          dsaGroup.AddEntry(nonStandardLengthPassEntry, true);

          PwGroup puttyGroup = new PwGroup(true, true, "Putty", PwIcon.Key);
          puttyGroup.AddGroup(rsaGroup, true);
          puttyGroup.AddGroup(dsaGroup, true);

          pluginHost.Database.RootGroup.AddGroup(puttyGroup, true);

          pluginHost.MainWindow.Invoke(new MethodInvoker(delegate()
          {
            pluginHost.MainWindow.UpdateUI(false, null, true, puttyGroup, true, puttyGroup, false);
            keeAgent.Initialize(pluginHost);
            pluginHost.MainWindow.FormClosing += delegate(Object source, FormClosingEventArgs args)
            {
              keeAgent.Terminate();
            };            
          }));
          while (KeePass.Program.MainForm != null &&
              KeePass.Program.MainForm.Visible == true) {
            Thread.Sleep(500);
          }
        });
      }
      KeePassControl.ExitAll();
    }
  }
}