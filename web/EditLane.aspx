<%@ Page MasterPageFile="~/Master.master" Language="C#" AutoEventWireup="true" CodeFile="EditLane.aspx.cs"
    Inherits="EditLane" %>

<asp:Content ID="Content2" ContentPlaceHolderID="content" runat="Server">
    <script type="text/javascript" src="EditLane.js"></script>
    <h2><asp:Label ID="lblH2" runat="server"></asp:Label></h2>
    <div>
        <asp:Table runat="server">
            <asp:TableRow>
                <asp:TableCell>Id:</asp:TableCell><asp:TableCell>
                    <asp:TextBox ID="txtID" runat="server" ReadOnly="True" Width="600px">0</asp:TextBox></asp:TableCell></asp:TableRow>
            <asp:TableRow>
                <asp:TableCell>Lane:</asp:TableCell><asp:TableCell>
                    <asp:TextBox ID="txtLane" runat="server" Width="600px"></asp:TextBox></asp:TableCell><asp:TableCell>The name of this lane. Must be unique on this server.</asp:TableCell></asp:TableRow>
            <asp:TableRow>
                <asp:TableCell>Source control:</asp:TableCell><asp:TableCell>
                    <asp:TextBox ID="txtSourceControl" runat="server" Width="600px">svn</asp:TextBox></asp:TableCell><asp:TableCell>Source control. Leave as 'svn'.</asp:TableCell></asp:TableRow>
            <asp:TableRow>
                <asp:TableCell>Repository:</asp:TableCell><asp:TableCell>
                    <asp:TextBox ID="txtRepository" runat="server" Width="600px"></asp:TextBox></asp:TableCell><asp:TableCell>The repository where the code for this lane is located. The scheduler uses the revisions for this repository to schedule work. Can include multiple repositories separated with commas.</asp:TableCell></asp:TableRow>
            <asp:TableRow>
                <asp:TableCell>Min revision:</asp:TableCell><asp:TableCell>
                    <asp:TextBox ID="txtMinRevision" runat="server" Width="600px"></asp:TextBox></asp:TableCell><asp:TableCell>Revisions before this one won't get scheduled.</asp:TableCell></asp:TableRow>
            <asp:TableRow>
                <asp:TableCell>Max revision:</asp:TableCell><asp:TableCell>
                    <asp:TextBox ID="txtMaxRevision" runat="server" Width="600px"></asp:TextBox></asp:TableCell><asp:TableCell>Revisions after this one won't get scheduled.</asp:TableCell></asp:TableRow>
            <asp:TableRow>
                <asp:TableCell>
                    <asp:Button ID="cmdSave" runat="server" Text="Save" OnClick="cmdSave_Click" /></asp:TableCell><asp:TableCell>
                    </asp:TableCell></asp:TableRow>
        </asp:Table>
        <br />
        <asp:Table runat="server" ID="tblDependencies" CssClass="buildstatus">
        </asp:Table>
        <br />
        <asp:Table runat="server" ID="tblCommands" CssClass="commands">
        </asp:Table>
        <br />
        <asp:Table runat="server" ID="tblFiles" CssClass="files">
        </asp:Table>
        <br />
        <asp:Table runat="server" ID="tblHosts" CssClass="buildstatus">
        </asp:Table>
    </div>
</asp:Content>
