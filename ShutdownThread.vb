'    WakeOnLAN - Wake On LAN
'    Copyright (C) 2004-2014 Aquila Technology, LLC. <webmaster@aquilatech.com>
'
'    This file is part of WakeOnLAN.
'
'    WakeOnLAN is free software: you can redistribute it and/or modify
'    it under the terms of the GNU General Public License as published by
'    the Free Software Foundation, either version 3 of the License, or
'    (at your option) any later version.
'
'    WakeOnLAN is distributed in the hope that it will be useful,
'    but WITHOUT ANY WARRANTY; without even the implied warranty of
'    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
'    GNU General Public License for more details.
'
'    You should have received a copy of the GNU General Public License
'    along with WakeOnLAN.  If not, see <http://www.gnu.org/licenses/>.

Imports System.ComponentModel
Imports WOL

Public Class ShutdownThread
    Public Enum ShutdownAction
        None
        Abort
        Shutdown
        Sleep
        Hibernate
        User
        Logoff
    End Enum

    Private WithEvents _backgroundWorker As New BackgroundWorker
    Private ReadOnly _item As ListViewItem
    Private ReadOnly _progressbar As ProgressBar
    Private ReadOnly _action As ShutdownAction
    Private ReadOnly _force As Boolean
    Private ReadOnly _reboot As Boolean
    Private _errMessage As String

    Public Sub New(ByVal item As ListViewItem, ByVal progressbar As ProgressBar, ByVal action As ShutdownAction, ByVal message As String, ByVal delay As Integer, ByVal force As Boolean, ByVal reboot As Boolean)
        _item = item
        _progressbar = progressbar
        _action = action
        _message = message
        _delay = delay
        _force = force
        _reboot = reboot
        _errMessage = ""
        _backgroundWorker.RunWorkerAsync()
    End Sub

    Private Sub DoWork(ByVal sender As Object, ByVal e As DoWorkEventArgs) Handles _backgroundWorker.DoWork
        Dim machine As Machine
        Dim flags As AquilaWolLibrary.ShutdownFlags
        Dim encryption As New Encryption(My.Application.Info.ProductName)

        machine = Machines(_item.Text)
        _item.SubItems(1).ForeColor = Color.FromKnownColor(KnownColor.WindowText)

        If (_action <> ShutdownAction.Abort And machine.ShutdownCommand.Length > 0) Then
            Shell(machine.ShutdownCommand, AppWinStyle.Hide, False)
            Return
        End If

        Try
            Select Case _action
                Case ShutdownAction.Shutdown
                    If (_force) Then
                        If (_reboot) Then
                            flags = AquilaWolLibrary.ShutdownFlags.ForcedReboot
                        Else
                            flags = AquilaWolLibrary.ShutdownFlags.ForcedShutdown
                        End If
                    Else
                        If (_reboot) Then
                            flags = AquilaWolLibrary.ShutdownFlags.Reboot
                        Else
                            flags = AquilaWolLibrary.ShutdownFlags.Shutdown
                        End If
                    End If

                Case ShutdownAction.Sleep
                    flags = AquilaWolLibrary.ShutdownFlags.Sleep

                Case ShutdownAction.Hibernate
                    flags = AquilaWolLibrary.ShutdownFlags.Hibernate

                Case ShutdownAction.Logoff
                    If (_force) Then
                        flags = AquilaWolLibrary.ShutdownFlags.ForcedLogoff
                    Else
                        flags = AquilaWolLibrary.ShutdownFlags.Logoff
                    End If

            End Select

            AquilaWolLibrary.Shutdown(machine.Netbios, flags, machine.UserID, encryption.EnigmaDecrypt(machine.Password), machine.Domain)

        Catch ex As Exception
            _errMessage = ex.Message
            e.Result = 1
            Return

        End Try

        e.Result = 0

    End Sub

    Private Sub backgroundWorker_RunWorkerCompleted(ByVal sender As Object, ByVal e As RunWorkerCompletedEventArgs) Handles _backgroundWorker.RunWorkerCompleted

        With _item.SubItems(1)
            If e.Result <> 0 Then
                .ForeColor = Color.Red
                .Text = String.Format(My.Resources.Strings.ErrorMsg, _errMessage)
                .Tag = .Text ' error
            Else
                .ForeColor = Color.Green
                .Text = My.Resources.Strings.Successful
                .Tag = String.Empty ' success
            End If
        End With

        With _progressbar
            .Increment(1)
            If (ShutdownMode = True) And (.Value = .Maximum) Then
                Shutdown.Complete()
            End If
        End With

    End Sub

End Class
