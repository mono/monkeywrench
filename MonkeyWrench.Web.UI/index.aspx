<%@ Page Language="C#" MasterPageFile="~/Master.master" AutoEventWireup="true" Inherits="index" Codebehind="index.aspx.cs" EnableViewState="false" %>

<asp:Content ID="Content1" ContentPlaceHolderID="content" runat="Server" EnableViewState="False">
    <h3>
        Build matrix</h3>
    <div id="buildtable" runat="server" />
    <div><asp:Label runat="server" ID="lblMessage" ForeColor="Red" /></div>
    <div><a href="SelectLanes.aspx">Select lanes</a></div>
    <h3>
        Legend</h3>
    <div>
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
</asp:Content>
