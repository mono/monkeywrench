<%@ Page Title="" Language="C#" MasterPageFile="~/Master.master" AutoEventWireup="true"
    CodeFile="index.aspx.cs" Inherits="index" %>

<asp:Content ID="Content1" ContentPlaceHolderID="content" runat="Server" EnableViewState="False">
    <h3>
        Build matrix</h3>
    <div id="buildtable" runat="server" enableviewstate="False">
    </div>
    <h3>
        Legend</h3>
    <div>
        <table class='buildstatus'>
            <tr>
                <td class='enabled'>
                    Enabled lane/host
                </td>
                <td class='disabled'>
                    Disabled lane/host
                </td>
            </tr>
        </table>
        <table class='buildstatus'>
            <tr>
                <td class='success'>
                    Success
                </td>
                <td class='issues'>
                    Issues (test failures)
                </td>
                <td class='aborted'>
                    Aborted
                </td>
                <td class='executing'>
                    Executing
                </td>
                <td class='failed'>
                    Failed
                </td>
                <td class='notdone'>
                    Queued
                </td>
                <td class='paused'>
                    Paused
                </td>
                <td class='skipped'>
                    Skipped
                </td>
                <td class='timeout'>
                    Timeout
                </td>
            </tr>
        </table>
    </div>
    <h3 runat="server" id="adminlinksheader" enableviewstate="false"></h3>
    <div runat="server" id="adminlinks" enableviewstate="false"></div>
</asp:Content>
