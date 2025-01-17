﻿// SPDX-License-Identifier: GPL-2.0-only
// Copyright (c) 2012-2017,2022 David Lechner <david@lechnology.com>

using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using dlech.SshAgentLib;
using KeePass.UI;
using System.IO;
using KeePassLib.Utility;

namespace KeeAgent.UI
{
  public partial class OptionsPanel : UserControl
  {
    KeeAgentExt ext;
    CheckedLVItemDXList optionsList;
    bool isUnix;

    public OptionsPanel(KeeAgentExt ext)
    {
      InitializeComponent();
      if (Type.GetType("Mono.Runtime") != null) {
        const int xOffset = -30;
        const int yOffset = -30;
        helpButton.Left += xOffset;
        customListViewEx.Width += xOffset;
        groupBox1.Width += xOffset;
        groupBox1.Height += yOffset;
      }

      this.ext = ext;
      isUnix = Environment.OSVersion.Platform == PlatformID.Unix
        || Environment.OSVersion.Platform == PlatformID.MacOSX;

      // make transparent so tab styling shows
      SetStyle(ControlStyles.SupportsTransparentBackColor, true);
      BackColor = Color.Transparent;

      modeComboBox.Items.Add(Translatable.OptionAgentModeAuto);
      modeComboBox.Items.Add(Translatable.OptionAgentModeAgent);
      modeComboBox.Items.Add(Translatable.OptionAgentModeClient);
      switch (ext.Options.AgentMode) {
        case AgentMode.Client:
          modeComboBox.SelectedItem = Translatable.OptionAgentModeClient;
          break;
        case AgentMode.Server:
          modeComboBox.SelectedItem = Translatable.OptionAgentModeAgent;
          break;
        default:
          modeComboBox.SelectedItem = Translatable.OptionAgentModeAuto;
          break;
      }

      // additional configuration of list view
      customListViewEx.UseCompatibleStateImageBehavior = false;
      UIUtil.SetExplorerTheme(customListViewEx, false);

      optionsList = new CheckedLVItemDXList(customListViewEx, true);
      var optionsGroup = new ListViewGroup("common", "Options");
      customListViewEx.Groups.Add(optionsGroup);
      optionsList.CreateItem(ext.Options, "IgnoreMissingExternalKeyFiles",
        optionsGroup, Translatable.OptionIgnoreMissingExternalKeyFiles);

      var agentModeOptionsGroup = new ListViewGroup("agentMode",
                          "Agent Mode Options (no effect in Client Mode)");
      customListViewEx.Groups.Add(agentModeOptionsGroup);
      optionsList.CreateItem(ext.Options, "AlwaysConfirm", agentModeOptionsGroup,
        Translatable.OptionAlwaysConfirm);
      optionsList.CreateItem(ext.Options, "ShowBalloon", agentModeOptionsGroup,
        Translatable.OptionShowBalloon);
      //mOptionsList.CreateItem(aExt.Options, "LoggingEnabled", optionsGroup,
      //  Translatable.optionLoggingEnabled);
      optionsList.CreateItem(ext.Options, "UnlockOnActivity", agentModeOptionsGroup,
       Translatable.OptionUnlockOnActivity);
      optionsList.CreateItem(ext.Options, "UserPicksKeyOnRequestIdentities",
        agentModeOptionsGroup, Translatable.OptionUserPicksKeyOnRequestIdentities);
      if (!isUnix) {
        optionsList.CreateItem(ext.Options, "UseWindowsOpenSshPipe",
          agentModeOptionsGroup, Translatable.OptionUseWindowsOpenSshPipe);
      }

      columnHeader.Width = customListViewEx.ClientRectangle.Width -
        UIUtil.GetVScrollBarWidth() - 1;
      if (isUnix) {
        groupBox1.Text = "Agent mode socket file";
        useCygwinSocketCheckBox.Visible = false;
        cygwinSocketPathTextBox.Text = ext.Options.UnixSocketPath;
        cygwinSocketPathTextBox.Enabled = true;
        cygwinPathBrowseButton.Enabled = true;
        useMsysSocketCheckBox.Visible = false;
        msysSocketPathLabel.Visible = false;
        msysSocketPathTextBox.Visible = false;
        msysPathBrowseButton.Visible = false;
        useWslSocketCheckBox.Visible = false;
        wslSocketPathLabel.Visible = false;
        wslSocketPathTextBox.Visible = false;
        wslSocketPathBrowseButton.Visible = false;
      }
      else {
        useCygwinSocketCheckBox.Checked = ext.Options.UseCygwinSocket;
        cygwinSocketPathTextBox.Text = ext.Options.CygwinSocketPath;
        useMsysSocketCheckBox.Checked = ext.Options.UseMsysSocket;
        msysSocketPathTextBox.Text = ext.Options.MsysSocketPath;
        useWslSocketCheckBox.Checked = ext.Options.UseWslSocket;
        wslSocketPathTextBox.Text = ext.Options.WslSocketPath;
      }
      optionsList.UpdateData(false);
    }

    protected override void OnLoad(EventArgs e)
    {
      base.OnLoad(e);
      if (ParentForm != null) {
        ParentForm.FormClosing += (sender, e2) => {
          if (ParentForm.DialogResult == DialogResult.OK) {
            if (!isUnix && useCygwinSocketCheckBox.Checked
              && string.IsNullOrWhiteSpace(cygwinSocketPathTextBox.Text)) {
              MessageService.ShowWarning("Must specify path for Cygwin socket file.");
              e2.Cancel = true;
              return;
            }
            if (!isUnix && useMsysSocketCheckBox.Checked
              && string.IsNullOrWhiteSpace(msysSocketPathTextBox.Text)) {
              MessageService.ShowWarning("Must specify path for MSYS socket file.");
              e2.Cancel = true;
              return;
            }
            if (!isUnix && useWslSocketCheckBox.Checked
              && string.IsNullOrWhiteSpace(wslSocketPathTextBox.Text)) {
              MessageService.ShowWarning("Must specify path for WSL socket file.");
              e2.Cancel = true;
              return;
            }
            if (isUnix && modeComboBox.Text != Translatable.OptionAgentModeClient
              && string.IsNullOrWhiteSpace(cygwinSocketPathTextBox.Text)) {
              MessageService.ShowWarning("Must specify path for Agent socket file.");
              e2.Cancel = true;
              return;
            }
            SaveChanges();
            if (ext.Options.UseCygwinSocket) {
              ext.StartCygwinSocket();
            }
            else {
              ext.StopCygwinSocket();
            }
            if (ext.Options.UseMsysSocket) {
              ext.StartMsysSocket();
            }
            else {
              ext.StopMsysSocket();
            }
            if (ext.Options.UseWslSocket) {
              ext.StartWslSocket();
            }
            else {
              ext.StopWslSocket();
            }
            if (ext.Options.UseWindowsOpenSshPipe) {
              ext.StartWindowsOpenSshPipe();
            }
            else {
              ext.StopWindowsOpenSsh();
            }
            if (isUnix) {
              ext.StartUnixSocket();
            }
          }
          optionsList.Release();
        };
      }
    }

    void SaveChanges()
    {
      optionsList.UpdateData(true);
      if (modeComboBox.Text == Translatable.OptionAgentModeAgent) {
        ext.Options.AgentMode = AgentMode.Server;
      }
      else if (modeComboBox.Text == Translatable.OptionAgentModeClient) {
        ext.Options.AgentMode = AgentMode.Client;
      }
      else {
        ext.Options.AgentMode = AgentMode.Auto;
      }
      if (isUnix) {
        ext.Options.UnixSocketPath = cygwinSocketPathTextBox.Text;
      }
      else {
        ext.Options.UseCygwinSocket = useCygwinSocketCheckBox.Checked;
        ext.Options.CygwinSocketPath = cygwinSocketPathTextBox.Text;
        ext.Options.UseMsysSocket = useMsysSocketCheckBox.Checked;
        ext.Options.MsysSocketPath = msysSocketPathTextBox.Text;
        ext.Options.UseWslSocket = useWslSocketCheckBox.Checked;
        ext.Options.WslSocketPath = wslSocketPathTextBox.Text;
      }
    }

    void helpButton_Click(object sender, EventArgs e)
    {
      Process.Start(Properties.Resources.WebHelpGlobalOptions);
    }

    void cygwinPathBrowseButton_Click(object sender, EventArgs e)
    {
      var file = browseForPath();
      if (file != null)
        cygwinSocketPathTextBox.Text = file;
    }

    void msysPathBrowseButton_Click(object sender, EventArgs e)
    {
      var file = browseForPath();
      if (file != null)
        msysSocketPathTextBox.Text = file;
    }

    void wslSocketPathBrowseButton_Click(object sender, EventArgs e)
    {
      var file = browseForPath();
      if (file != null)
        wslSocketPathTextBox.Text = file;
    }

    string browseForPath()
    {
      // TODO: Would be nice if we could change the name of the "OK" button from
      // "Save" to "Select".
      var dialog = new SaveFileDialog() {
        Title = "Enter Socket File Name",
        Filter = "All files (*.*)|*.*",
        CheckFileExists = false,
        OverwritePrompt = false,
      };
      dialog.FileOk += (s, e) => {
        if (File.Exists(dialog.FileName)) {
          MessageService.ShowWarning("File exists.", "Enter a new file name.");
          e.Cancel = true;
        }
      };
      if (dialog.ShowDialog() == DialogResult.Cancel) {
        return null;
      }
      return dialog.FileName;
    }

    void useCygwinSocketCheckBox_CheckedChanged(object sender, EventArgs e)
    {
      if (!isUnix) {
        cygwinSocketPathTextBox.Enabled = useCygwinSocketCheckBox.Checked;
        cygwinPathBrowseButton.Enabled = useCygwinSocketCheckBox.Checked;
      }
    }

    void useMsysSocketCheckBox_CheckedChanged(object sender, EventArgs e)
    {
      if (!isUnix) {
        msysSocketPathTextBox.Enabled = useMsysSocketCheckBox.Checked;
        msysPathBrowseButton.Enabled = useMsysSocketCheckBox.Checked;
      }
    }

    void useWslSocketCheckBox_CheckedChanged(object sender, EventArgs e)
    {
      if (!isUnix) {
        wslSocketPathTextBox.Enabled = useWslSocketCheckBox.Checked;
        wslSocketPathBrowseButton.Enabled = useWslSocketCheckBox.Checked;
      }
    }
  }
}
