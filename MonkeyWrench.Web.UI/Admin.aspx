<%@ Page Language="C#" MasterPageFile="~/Master.master" AutoEventWireup="true" CodeBehind="Admin.aspx.cs" Inherits="Admin" %>

<asp:Content ID="Content2" ContentPlaceHolderID="content" runat="Server">
    <div style="text-align: center; padding: 5px">
        Scheduler status:
        <asp:Label ID="lblSchedulerStatus" runat="server">(Unknown)</asp:Label><br />
        <asp:Button ID="cmdSchedule" runat="server" Text="Execute scheduler (full update)" OnClick="cmdSchedule_Click" />
        <br />
        <asp:Label ID="lblSchedule" runat="server" />
        <br /><br />
        Retention directives status:
        <asp:Label ID="lblDeletionDirectiveStatus" runat="server">(Unknown)</asp:Label><br />
        <asp:Button ID="cmdExecuteDeletionDirectives" runat="server" Text="Execute retention directives" OnClick="cmdExecuteDeletionDirectives_Click" />
        <br />
        <asp:Label ID="lblExecuteDeletionDirectives" runat="server" />
        <asp:Label ID="lblMessage" runat="server" ForeColor="Red" />
    </div>
</asp:Content>
