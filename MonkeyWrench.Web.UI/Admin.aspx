<%@ Page Language="C#" MasterPageFile="~/Master.master" AutoEventWireup="true" CodeBehind="Admin.aspx.cs" Inherits="MonkeyWrench.Web.UI.Admin" %>

<asp:Content ID="Content2" ContentPlaceHolderID="content" runat="Server">
    <div style="text-align:center; padding: 5px">
        <asp:Button ID="cmdSchedule" runat="server" Text="Execute scheduler (full update)"/>
        <br />
        <asp:Label ID="lblSchedule" runat="server" />
        <br />
        <asp:Button ID="cmdExecuteDeletionDirectives" runat="server" Text="Execute retention directives" />
        <br />
        <asp:Label ID="lblExecuteDeletionDirectives" runat="server" />        
    </div>
</asp:Content>
