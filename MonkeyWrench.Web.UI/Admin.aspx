<%@ Page Language="C#" MasterPageFile="~/Master.master" AutoEventWireup="true" CodeBehind="Admin.aspx.cs" Inherits="MonkeyWrench.Web.UI.Admin" %>

<asp:Content ID="Content2" ContentPlaceHolderID="content" runat="Server">
    <div>
        <asp:Button ID="cmdSchedule" runat="server" Text="Execute scheduler (full update)" />
        <asp:Label ID="lblSchedule" runat="server" />
    </div>
</asp:Content>
