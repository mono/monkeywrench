<%@ Page MasterPageFile="~/Master.master" Language="C#" AutoEventWireup="true" Inherits="EditLane" CodeBehind="EditLane.aspx.cs" %>
<%@ Register Src="~/EnvironmentVariablesEditor.ascx"  TagName="EnvironmentVariablesEditor" TagPrefix="ucs" %>

<asp:Content ID="Content2" ContentPlaceHolderID="content" runat="Server">

    <script type="text/javascript" src="EditLane.js"></script>
    <script type="text/javascript" src="MonkeyWrench.js"></script>

    <h2>
        <asp:Label ID="lblH2" runat="server"></asp:Label></h2>
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
                    <asp:TextBox ID="txtMinRevision" runat="server" Width="600px"></asp:TextBox></asp:TableCell><asp:TableCell>Revisions before this one won't get scheduled. Leave blank to include all revisions.</asp:TableCell></asp:TableRow>
            <asp:TableRow>
                <asp:TableCell>Max revision:</asp:TableCell>
                <asp:TableCell>
                    <asp:TextBox ID="txtMaxRevision" runat="server" Width="600px"></asp:TextBox>
                </asp:TableCell>
                <asp:TableCell>Revisions after this one won't get scheduled. Leave blank to include all (future) revisions.</asp:TableCell>
            </asp:TableRow>
            <asp:TableRow>
                <asp:TableCell>Parent lane:</asp:TableCell>
                <asp:TableCell>
                    <asp:DropDownList ID="lstParentLane" runat="server" Width="600px"></asp:DropDownList>
                </asp:TableCell>
                <asp:TableCell>The (optional) parent lane of this lane.</asp:TableCell>
            </asp:TableRow>
            <asp:TableRow>
                <asp:TableCell>Child lanes:</asp:TableCell>
                <asp:TableCell>
                    <asp:Label ID="lblChildLanes" runat="server" Width="600px"></asp:Label>
                </asp:TableCell>
                <asp:TableCell></asp:TableCell>
            </asp:TableRow>
            <asp:TableRow>
                <asp:TableCell>
                    <asp:Button ID="cmdSave" runat="server" Text="Save" OnClick="cmdSave_Click" /></asp:TableCell><asp:TableCell>
                    </asp:TableCell>
            </asp:TableRow>
        </asp:Table>
        <br />
        <asp:Table runat="server" ID="tblDependencies" CssClass="commands">
        </asp:Table>
        <br />
        <asp:Table runat="server" ID="tblCommands" CssClass="commands">
        </asp:Table>
        <br />
        <asp:Table runat="server" ID="tblFiles" CssClass="commands">
        </asp:Table>
        <br />
        <asp:Table runat="server" ID="tblDeletionDirective" CssClass="commands">
            <asp:TableHeaderRow>
                <asp:TableHeaderCell ColumnSpan="7"><a href="doc/EditLaneHelp.html#id_DeletionDirectives" target="_blank">Retention directives</a></asp:TableHeaderCell></asp:TableHeaderRow>
            <asp:TableHeaderRow>
                <asp:TableHeaderCell>Directive</asp:TableHeaderCell>
                <asp:TableHeaderCell>Filename</asp:TableHeaderCell>
                <asp:TableHeaderCell>Glob mode</asp:TableHeaderCell>
                <asp:TableHeaderCell>Condition</asp:TableHeaderCell>
                <asp:TableHeaderCell>X</asp:TableHeaderCell>
                <asp:TableHeaderCell>Enabled</asp:TableHeaderCell>
                <asp:TableHeaderCell></asp:TableHeaderCell>
            </asp:TableHeaderRow>
            <asp:TableRow>
                <asp:TableCell>
                    <asp:TextBox ID="txtDeletionDirective1" runat="server" Text="description" />
                </asp:TableCell>
                <asp:TableCell>
                    <asp:TextBox ID="txtDeletionDirectiveFilename1" Text="filename" runat="server" />
                </asp:TableCell>
                <asp:TableCell>
                    <asp:DropDownList ID="lstDeletionDirectiveGlobs1" runat="server" />
                </asp:TableCell>
                <asp:TableCell>
                    <asp:DropDownList ID="lstDeletionDirectiveCondition1" runat="server" />
                </asp:TableCell>
                <asp:TableCell>
                    <asp:TextBox ID="txtDeletionDirectiveX1" runat="server" />
                </asp:TableCell>
                <asp:TableCell>
                    <asp:CheckBox ID="chkDeletionDirectiveEnabled1" runat="server" />
                </asp:TableCell>
                <asp:TableCell>
                    <asp:LinkButton ID="lnkAddDeletionDirective1" CommandName="addDeletionDirective" CommandArgument="1" OnCommand="btn_Command" runat="server" Text="Add" />
                </asp:TableCell>
            </asp:TableRow>
            <asp:TableRow>
                <asp:TableCell>
                    <asp:DropDownList ID="lstDeletionDirectives2" runat="server" />
                </asp:TableCell>
                <asp:TableCell>
                    -
                </asp:TableCell>
                <asp:TableCell>
                    -
                </asp:TableCell>
                <asp:TableCell>
                    -
                </asp:TableCell>
                <asp:TableCell>
                    -
                </asp:TableCell>
                <asp:TableCell>
                    <asp:CheckBox ID="chkDeletionDirectiveEnabled2" runat="server" />
                </asp:TableCell>
                <asp:TableCell>
                    <asp:LinkButton ID="lnkAddDeletionDirective2" CommandName="addDeletionDirective" CommandArgument="2" OnCommand="btn_Command" runat="server" Text="Link" />
                </asp:TableCell>
            </asp:TableRow>
        </asp:Table>
        <asp:Label Visible="false" ID="lblDeletionDirectiveErrors" runat="server" ForeColor="Red" />
        <br />
        <asp:Table runat="server" ID="tblHosts" CssClass="commands">
        </asp:Table>
        <br />
        <ucs:EnvironmentVariablesEditor ID="editorVariables" runat="server"></ucs:EnvironmentVariablesEditor>
        <br />
    </div>
</asp:Content>
