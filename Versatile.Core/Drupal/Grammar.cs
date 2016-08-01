﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

using Sprache;

namespace Versatile
{
    public partial class Drupal
    {
        public class Grammar : Versatile.Grammar<Drupal>
        {
            public static Parser<string> CoreIdentifier
            {
                get
                {
                    return
                        from digits in Digits
                        from minor in MinorX
                        select digits;
                          
                }
            }

            public static Parser<List<string>> PreReleaseIdentifier
            {
                get
                {

                    return
                        from dash in Dash.Or(Parse.Char('_'))
                        from s in Parse.String("dev").Or(Parse.String("unstable")).Or(Parse.String("alpha"))
                            .Or(Parse.String("beta")).Or(Parse.String("rc")).Or(Parse.String("rev")).Text()
                        from d in NumericIdentifier.Optional().Select(o => o.GetOrElse(s == "dev"? string.Empty : "0"))
                        let has_number = !string.IsNullOrEmpty(d)
                        select has_number ? new List<string> { s, d } : new List<string> { s };
                }
            }

            public static Parser<List<string>> ContribIdentifier
            {
                get
                {
                    return
                        from c in CoreIdentifier
                        from dash in Dash
                        from major in Major
                        from patch in Patch
                        from pre in PreReleaseIdentifier.Optional().Select(p => p.GetOrElse(null))
                        let has_pre = pre is List<string> 
                        select has_pre ? new List<string> {c, major, "", patch, string.Join(".", pre) } : new List<string> {c, major, "", patch };
                }
            }

            public static Parser<List<string>> ContribIdentifierWithPatchXIdentifier
            {
                get
                {
                    return
                        from c in CoreIdentifier
                        from dash in Dash
                        from major in Major
                        from dot in Dot
                        from x in XIdentifier
                        from pre in PreReleaseIdentifier.Optional().Select(p => p.GetOrElse(null))
                        let has_pre = pre is List<string>
                        select has_pre ? new List<string> { c, major, "", "0", string.Join(".", pre) } : new List<string> { c, major, "", "0" };
                }
            }

            public static Parser<List<string>> ContribIdentifierWithoutCoreIdentitifier
            {
                get
                {
                    return
                        from d in NumericIdentifier.DelimitedBy(Dot).Select(nid => nid.ToList())
                        from prerelease in PreReleaseIdentifier.Optional()
                        let has2 = d.Count() == 2
                        let d2 = has2 ? new List<string> { d[0], d[1], "0", "0", prerelease.IsDefined ? string.Join(".", prerelease.Get()) : string.Empty } : null
                        let has3 = d.Count() == 3
                        let d3 = has3 ? new List<string> { d[0], d[1], "0", d[2], prerelease.IsDefined ? string.Join(".", prerelease.Get()) : string.Empty } : null
                        select has2 ? d2 : has3 ? d3 : d.Take(5).ToList();
                }
            }

            public static Parser<List<string>> ContribIdentifierWithCoreIdentitifierOnly
            {
                get
                {
                    return
                        from c in CoreIdentifier
                        select new List<string> { c, "0", "0", "0" }; 
                    
                }
            }

            public static Parser<Drupal> DrupalVersion
            {
                get
                {
                    return
                        from dv in ContribIdentifierWithPatchXIdentifier.Or(ContribIdentifier)
                        .Or(ContribIdentifierWithCoreIdentitifierOnly).Or(ContribIdentifierWithoutCoreIdentitifier)
                        select new Drupal(dv);
                }
            }

            public static Parser<Composer> DrupalToComposerVersion
            {
                get
                {
                    return
                        from d in DrupalVersion
                        select new Composer(d.Major, d.Minor, d.Patch, d.PreRelease.ToString());
                }
            }

            public static Parser<ComparatorSet<Drupal>> TwoSidedIntervalRange
            {
                get
                {
                    return
                        from le in OneSidedIntervalOperator
                        from l in DrupalVersion.Token()
                        from a in Parse.WhiteSpace.Or(Ampersand).Token().Optional()
                        from re in OneSidedIntervalOperator
                        from r in DrupalVersion.Token()
                        select new ComparatorSet<Drupal>
                        {
                            new Comparator<Drupal>(le,  l),
                            new Comparator<Drupal>(re, r)
                        };
                }
            }

            public static Parser<ComparatorSet<Drupal>> BracketedTwoSidedIntervalRange
            {
                get
                {
                    return
                    from b1 in OpenBracket
                    from d in TwoSidedIntervalRange
                    from b2 in ClosedBracket
                    select d;
                }
            }

            public static Parser<ComparatorSet<Drupal>> OneOrTwoSidedRange
            {
                get
                {
                    return BracketedTwoSidedIntervalRange.Or(TwoSidedIntervalRange).Or(OneSidedRange);
                }
            }

            public static Parser<List<ComparatorSet<Drupal>>> Range
            {
                get
                {
                    return CommaDelimitedRange.Or(OneOrTwoSidedRange.DelimitedBy(Parse.String("||").Token())).Select(r => r.ToList());
                }
            }



        }
    }
}
