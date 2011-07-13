<%@ Page Language="C#" MasterPageFile="~/Master.master" AutoEventWireup="true" Inherits="ViewServerLog" CodeBehind="ViewServerLog.aspx.cs" %>

<asp:Content ID="Content2" ContentPlaceHolderID="content" runat="Server">
    <asp:Label ID='lblLength' runat="server" /> - 
    <a href="ViewServerLog.aspx?maxlength=4096">Last 4kb</a> - 
    <a href="ViewServerLog.aspx?maxlength=32768">Last 32kb</a> - 
    <a href="ViewServerLog.aspx?maxlength=262144">Last 256kb</a> -
    <a href="ViewServerLog.aspx?maxlength=1048576">Last 1024kb</a>
    <br />
    <br />
    <asp:Label ID='divLog' runat="server" />
    <br />
    <a href="ViewServerLog.aspx?maxlength=4096">Last 4kb</a> - 
    <a href="ViewServerLog.aspx?maxlength=32768">Last 32kb</a> - 
    <a href="ViewServerLog.aspx?maxlength=262144">Last 256kb</a> -
    <a href="ViewServerLog.aspx?maxlength=1048576">Last 1024kb</a>
</asp:Content>

