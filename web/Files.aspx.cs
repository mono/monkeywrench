/*
 *
 * Contact:
 *   Moonlight List (moonlight-list@lists.ximian.com)
 *
 * Copyright 2008 Novell, Inc. (http://www.novell.com)
 *
 * See the LICENSE file included with the distribution for details.
 *
 */

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using Builder;

public partial class Files : System.Web.UI.Page
{
	private new Master Master
	{
		get { return base.Master as Master; }
	}

	protected void Page_Load (object sender, EventArgs e)
	{
		try {
			Process ();
		} catch (Exception ex) {
			Response.Write (ex.Message);
			Response.Write (ex.StackTrace);
		}
	}

	private void Process ()
	{
		int start;
		int page_size;
		int page_no;

		if (Master.Login == null) {
			Response.Redirect ("Login.aspx");
			return;
		}

		if (!int.TryParse (Request ["start"], out start))
			start = -1;

		if (!int.TryParse (Request ["page_size"], out page_size))
			page_size = 50;

		if (!int.TryParse (Request ["page_no"], out page_no))
			page_no = 0;

		using (DB db = new DB (true)) {
			using (DB db_size = new DB (true)) {
				if (start == -1) {
					object o = db.ExecuteScalar ("SELECT min (loid) FROM pg_largeobject;");
					if (o is int)
						start = (int) o;
					else if (o is long)
						start = (int) (long) o;
					else
						start = 0;
				}

				Console.WriteLine ("start: {0}, page size: {1}, page no: {2}", start, page_size, page_no);

				using (IDbCommand cmd = db.Connection.CreateCommand ()) {
					cmd.CommandText = @"
SELECT 
	pg_largeobject.loid, 
	File.filename AS file_filename, File.size, File.file_id,
	WorkFile.id AS workfile_id, WorkFile.filename AS workfile_filename,
	RevisionWork.state, 
	Revision.revision,
	Lane.lane,
	Host.host
FROM pg_largeobject 
LEFT JOIN File ON File.file_id = pg_largeobject.loid 
LEFT JOIN WorkFile ON File.id = WorkFile.file_id 
LEFT JOIN Work ON WorkFile.work_id = Work.id
LEFT JOIN RevisionWork ON RevisionWork.id = Work.revisionwork_id
LEFT JOIN Revision ON RevisionWork.revision_id = Revision.id
LEFT JOIN Lane ON Lane.id = RevisionWork.lane_id
LEFT JOIN Host ON RevisionWork.host_id = Host.id
WHERE loid >= @start AND loid <= @end AND pageno = 0
ORDER BY loid;
";

					DB.CreateParameter (cmd, "start", (start + page_no * page_size).ToString ());
					DB.CreateParameter (cmd, "end", (start + (page_no + 1) * page_size).ToString ());

					using (IDataReader reader = cmd.ExecuteReader ()) {
						long prev_loid = -1;

						int workfile_id_idx = reader.GetOrdinal ("workfile_id");
						int workfile_filename_idx = reader.GetOrdinal ("workfile_filename");
						int loid_idx = reader.GetOrdinal ("loid");
						int file_filename_idx = reader.GetOrdinal ("file_filename");
						int size_idx = reader.GetOrdinal ("size");
						int revision_idx = reader.GetOrdinal ("revision");
						int lane_idx = reader.GetOrdinal ("lane");
						int host_idx = reader.GetOrdinal ("host");
						int file_id_idx = reader.GetOrdinal ("file_id");

						while (reader.Read ()) {
							long loid = reader.GetInt64 (loid_idx);
							int size = reader.GetInt32 (size_idx);
							bool has_file = !reader.IsDBNull (file_filename_idx);
							bool has_workfile = !reader.IsDBNull (workfile_id_idx);
							int obj_size = -1;

							try {
								if (!has_file || loid != prev_loid)
									obj_size = db_size.GetLargeObjectSize ((int) loid);
							} catch (Exception ex) {
								// Ignore
								Logger.Log ("Exception while getting large object size: {0}\n{1}", ex.Message, ex.StackTrace);
							}

							if (!has_file) {
								tblFiles.Rows.Add (Utils.CreateTableRow (loid.ToString (), "<no files>", obj_size.ToString () + " bytes"));
								continue;
							}

							if (loid != prev_loid) {
								string file_filename = reader.GetString (file_filename_idx);
								int file_id = reader.GetInt32 (file_id_idx);
								tblFiles.Rows.Add (Utils.CreateTableRow (loid.ToString (), file_id.ToString (), file_filename, obj_size.ToString () + " bytes"));
							}

							if (has_workfile) {
								int workfile_id = reader.GetInt32 (workfile_id_idx);
								string workfile_filename = reader.GetString (workfile_filename_idx);
								string revision = reader.GetString (revision_idx);
								string lane = reader.GetString (lane_idx);
								string host = reader.GetString (host_idx);
								tblFiles.Rows.Add (Utils.CreateTableRow (
										">",
										workfile_id.ToString (),
										lane,
										host,
										revision,
										workfile_filename,
										size.ToString () + " bytes"));
							}

							prev_loid = loid;
						}
					}

					tblFiles.Rows.Add (Utils.CreateTableRow (
						page_no > 0 ? string.Format ("<a href='Files.aspx?start={0}&amp;page_size={1}&amp;page_no={2}'>Previous</a>", start, page_size, page_no - 1) : "-",
						string.Format ("<a href='Files.aspx?start={0}&amp;page_size={1}&amp;page_no={2}'>Next</a>", start, page_size, page_no + 1),
						""));
				}
			}
		}
	}
}
