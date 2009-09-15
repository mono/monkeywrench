<%@ Page Language="C#" MasterPageFile="~/Master.master" AutoEventWireup="true" CodeBehind="SelectLanes.aspx.cs" Inherits="SelectLanes" %>

<asp:Content ID="Content1" ContentPlaceHolderID="content" runat="Server" EnableViewState="False">
    <asp:Table ID="tblLanes" runat="server">
        <asp:TableHeaderRow>
            <asp:TableHeaderCell ColumnSpan="2">Select lanes to view</asp:TableHeaderCell>
        </asp:TableHeaderRow>
        <asp:TableFooterRow>
            <asp:TableCell ColumnSpan="2">
                <asp:Button ID="cmdOK" runat="server" Text="OK" OnClick="cmdOK_OnClick" UseSubmitBehavior="False" />
            </asp:TableCell>
        </asp:TableFooterRow>
    </asp:Table>
</asp:Content>
