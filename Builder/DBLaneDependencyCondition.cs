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
using System.Text;

namespace Builder
{
	public enum DBLaneDependencyCondition
	{
		Invalid = 0,
		DependentLaneSuccess = 1,
		DependentLaneSuccessWithFile = 2
	}
}
