﻿//
// BuildLoggingWidget.cs
//
// Author:
//       Matt Ward <matt.ward@microsoft.com>
//
// Copyright (c) 2019 Microsoft Corporation
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.IO;
using MonoDevelop.Components;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Projects;
using Xwt;

namespace MonoDevelop.ProjectSystem.Tools.Gui
{
	partial class BuildLoggingWidget
	{
		public BuildLoggingWidget ()
		{
			Build ();

			listView.RowActivated += ListViewRowActivated;
			listView.ButtonPressed += ListViewButtonPressed;
		}

		public void ClearItems ()
		{
			DeleteMSBuildOutputFiles ();
			listStore.Clear ();
		}

		public void AddMSBuildTarget (MSBuildTarget target)
		{
			int row = listStore.AddRow ();

			listStore.SetValues (
				row,

				projectDataField,
				target.ProjectFileName,

				dimensionsDataField,
				target.Dimensions,

				targetsDataField,
				target.Targets,

				typeDataField,
				target.BuildType,

				startDataField,
				target.StartTime.ToString ("s"),

				statusDataField,
				target.Status.GetDisplayText (),

				msbuildTargetDataField,
				target);
		}

		public void UpdateMSBuildTarget (MSBuildTarget target)
		{
			for (int row = listStore.RowCount - 1; row >= 0; --row) {
				MSBuildTarget currentTarget = listStore.GetValue (row, msbuildTargetDataField);
				if (target == currentTarget) {
					listStore.SetValues (
						row,

						elapsedDataField,
						target.Duration.ToString ("g"),

						statusDataField,
						target.Status.GetDisplayText ());

					return;
				}
			}
		}

		protected override void Dispose (bool disposing)
		{
			base.Dispose (disposing);

			if (disposing) {
				listView.RowActivated -= ListViewRowActivated;
				listView.ButtonPressed -= ListViewButtonPressed;

				DeleteMSBuildOutputFiles ();
			}
		}

		void DeleteMSBuildOutputFiles ()
		{
			MSBuildTarget target = null;

			for (int row = 0; row < listStore.RowCount; ++row) {
				try {
					target = listStore.GetValue (row, msbuildTargetDataField);
					if (target.LogFileName.IsNotNull) {
						File.Delete (target.LogFileName);
					}
				} catch (Exception ex) {
					LoggingService.LogError (
						string.Format ("Unable to remove msbuild output file {0}", target),
						ex);
				}
			}
		}

		void ListViewRowActivated (object sender, ListViewRowEventArgs e)
		{
			OpenLogFileForRow (e.RowIndex);
		}

		void OpenLogFileForRow (int row)
		{
			if (row < 0) {
				return;
			}

			MSBuildTarget target = listStore.GetValue (row, msbuildTargetDataField);
			if (target == null) {
				return;
			}

			if (target.LogFileName.IsNotNull && File.Exists (target.LogFileName)) {
				IdeApp.Workbench.OpenDocument (target.LogFileName, (Project)null)
					.Ignore ();
			}
		}

		void ListViewButtonPressed (object sender, ButtonEventArgs e)
		{
			if (!e.IsContextMenuTrigger) {
				return;
			}

			var commands = IdeApp.CommandService.CreateCommandEntrySet ("/MonoDevelop/BuildLoggingPad/ContextMenu");
			IdeApp.CommandService.ShowContextMenu (listView.ToGtkWidget (), (int)e.X, (int)e.Y, commands, this);
		}

		[CommandUpdateHandler (BuildLoggingCommands.OpenLogFile)]
		void OnUpdateOpenLogFile (CommandInfo info)
		{
			info.Enabled = IsMSBuildTargetSelected ();
		}

		[CommandHandler (BuildLoggingCommands.OpenLogFile)]
		void OpenLogFile ()
		{
			if (IsMSBuildTargetSelected ()) {
				OpenLogFileForRow (listView.SelectedRow);
			}
		}

		bool IsMSBuildTargetSelected ()
		{
			return listView.SelectedRow >= 0;
		}
	}
}
