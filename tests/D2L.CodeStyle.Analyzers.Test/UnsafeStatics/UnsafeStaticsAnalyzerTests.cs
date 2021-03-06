﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using D2L.CodeStyle.Analyzers.Test.Verifiers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using NUnit.Framework;

namespace D2L.CodeStyle.Analyzers.UnsafeStatics {

	internal sealed class UnsafeStaticsAnalyzerTests : DiagnosticVerifier {
		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() {
			return new UnsafeStaticsAnalyzer();
		}

		[Test]
		public void EmptyDocument_NoDiag() {
			const string test = @"";

			VerifyCSharpDiagnostic( test );
		}

		[Test]
		public void DocumentWithoutStatic_NoDiag() {
			const string test = @"
    using System;

    namespace test {
        class Tests {

            public DateTime good = DateTime.Now;
            public DateTime goodToo { get; set; }

        }
    }";
			AssertNoDiagnostic( test );
		}

		[Test]
        public void DocumentWithStatic_ReadonlySelfReferencingStatic_NoDiag() {
            const string test = @"
    using System;

    namespace test {
        class Tests {

            public sealed class Foo {
                public static readonly Foo Default = new Foo();
            }
            public static readonly Foo good = new Foo();

        }
    }";
            AssertNoDiagnostic( test );
        }

        [Test]
        public void DocumentWithStatic_ReadonlySelfReferencingStaticOfMutableType_Diag() {
            const string test = @"
    using System;

    namespace test {
        class Tests {

            public sealed class Foo {
                private int uhoh = 1;
                public static readonly Foo Default = new Foo();
            }
            public static readonly Foo good = new Foo();

        }
    }";
            var diag1 = CreateDiagnosticResult( 9, 44, "Default", "test.Tests.Foo" );
            var diag2 = CreateDiagnosticResult( 11, 40, "good", "test.Tests.Foo" );
            VerifyCSharpDiagnostic( test, diag1, diag2 );
        }

        [Test]
		public void DocumentWithStaticField_NonReadonly_Diag() {
			const string test = @"
    using System;

    namespace test {
        class Tests {

            internal sealed class Foo {
                public readonly string ClientsName = ""YOLO"";
            }

            public static Foo bad = new Foo();

        }
    }";
			AssertSingleDiagnostic( test, 11, 31, "bad", "it" );
		}

		[Test]
		public void DocumentWithStaticField_NonReadonlyUnaudited_NoDiag() {
			const string test = @"
    using System;

    namespace test {
        class Tests {

            internal class Foo {
                public readonly string ClientsName = ""YOLO"";
            }

            [CodeStyle.Statics.Unaudited]
            public static Foo bad = new Foo();

        }
    }";
			AssertNoDiagnostic( test );
		}


		[Test]
		public void DocumentWithStaticField_NonReadonlyAudited_NoDiag() {
			const string test = @"
    using System;

    namespace test {
        class Tests {

            internal class Foo {
                public readonly string ClientsName = ""YOLO"";
            }

            [CodeStyle.Statics.Audited]
            public static Foo bad = new Foo();

        }
    }";
			AssertNoDiagnostic( test );
		}

		[Test]
		public void DocumentWithStaticField_ReadonlyButMutable_Diag() {
			const string test = @"
    using System;

    namespace test {
        class Tests {

            internal class Foo {
                public string ClientsName = ""YOLO"";
            }

            public static readonly Foo bad = new Foo();

        }
    }";
			AssertSingleDiagnostic( test, 11, 40, "bad", "test.Tests.Foo" );
		}

		[Test]
		public void DocumentWithStaticField_ReadonlyValueType_NoDiag() {
			const string test = @"
    using System;

    namespace test {
        class Tests {

            public static readonly DateTime good = DateTime.Now;

        }
    }";
			AssertNoDiagnostic( test );
		}

        [Test]
        public void DocumentWithStaticField_ReadonlyNotSealedImmutableUnknownConcreteType_NoDiag() {
            const string test = @"
    using System;

    namespace test {
        class Tests {

            internal class Foo {
                public readonly string ClientsName = ""YOLO"";
            }

            public static readonly Foo bad = GetFoo();

            private static Foo GetFoo() {
                return new Foo();
            }

        }
    }";

			// Although a concrete instance of Foo is safe, we don't look
			// inside GetFoo to see that its returning a concrete Foo and
			// not some derived class.
			AssertSingleDiagnostic( test, 11, 40, "bad", "test.Tests.Foo" );
		}

        [Test]
        public void DocumentWithStaticField_ReadonlyNotSealedImmutableKnownConcreteType_NoDiag() {
            const string test = @"
    using System;

    namespace test {
        class Tests {

            internal class Foo {
                public readonly string ClientsName = ""YOLO"";
            }

            public static readonly Foo bad = new Foo();

        }
    }";

            AssertNoDiagnostic( test );
        }

		[Test]
		public void DocumentWithStaticField_ReadonlySealedImmutable_NoDiag() {
			const string test = @"
    using System;

    namespace test {
        class Tests {

            internal sealed class Foo {
                public readonly string ClientsName = ""YOLO"";
            }

            public static readonly Foo bad = new Foo();

        }
    }";
			AssertNoDiagnostic( test );
		}

		[Test]
		public void DocumentWithStaticField_ImmutableFieldWithImmutableMarkedType_NoDiag() {
			const string test = @"
    using System;

    namespace test {
        class Tests {

            [Immutable] // yes, this isn't actually immutable, that's the point
            internal class Foo {
                public string ClientsName = ""YOLO"";
            }

            public static readonly Foo good;

        }
    }";
			AssertNoDiagnostic( test );
		}

        [Test]
        public void DocumentWithStaticField_InterfaceWithImmutableConcreteInitializer_NoDiag() {
            const string test = @"
    using System;

    namespace test {
        class Tests {

            interface IFoo {}
            internal sealed class Foo : IFoo {
                public readonly string ClientsName = ""YOLO"";
            }

            public readonly static IFoo good = new Foo();

        }
    }";
            AssertNoDiagnostic( test );
        }

        [Test]
        public void DocumentWithStaticField_InterfaceWithMutableConcreteInitializer_NoDiag() {
            const string test = @"
    using System;

    namespace test {
        class Tests {

            interface IFoo {}
            internal sealed class Foo : IFoo {
                public string ClientsName = ""YOLO"";
            }

            public readonly static IFoo bad = new Foo();

        }
    }";
            AssertSingleDiagnostic( test, 12, 41, "bad", "test.Tests.Foo" );
        }

        [Test]
        public void DocumentWithStaticCollectionField_NonGeneric_Diag() {
            const string test = @"
    using System;

    namespace test {
        class Tests {
            public static readonly System.Collections.IList bad;

        }
    }";
            AssertSingleDiagnostic( test, 6, 61, "bad", "System.Collections.IList" );
        }

        [Test]
        public void DocumentWithStaticCollectionField_GenericObject_Diag() {
            const string test = @"
    using System;

    namespace test {
        class Tests {
            public static readonly System.Collections.Generic.List<object> bad;

        }
    }";
            AssertSingleDiagnostic( test, 6, 76, "bad", "System.Collections.Generic.List<System.Object>" );
        }

        [Test]
        public void DocumentWithStaticImmutableCollectionField_GenericObject_Diag() {
            const string test = @"
    using System;

    namespace test {
        class Tests {
            public static readonly System.Collections.Immutable.ImmutableList<object> bad;

        }
    }";
            AssertSingleDiagnostic( test, 6, 87, "bad", "System.Collections.Immutable.ImmutableList<System.Object>" );
        }

        [Test]
        public void DocumentWithStaticImmutableCollectionField_GenericImmutableObject_Diag() {
            const string test = @"
    using System;

    namespace test {
        class Tests {
            public static readonly System.Collections.Immutable.ImmutableList<int> good;

        }
    }";
            AssertNoDiagnostic( test );
        }


        [Test]
        public void DocumentWithStaticImmutableCollectionField_GenericImmutableMarkedObject_Diag() {
            const string test = @"
    using System;

    namespace test {
        class Tests {
            [Objects.Immutable]
            class Foo {
                void MethodsMakesMeNotDeterministicallyImmutable() {}
            }
            public static readonly System.Collections.Immutable.ImmutableList<Foo> good;

        }
    }";
            AssertNoDiagnostic( test );
        }


        [Test]
		public void DocumentWithStaticProperty_NonReadonly_Diag() {
			const string test = @"
    using System;

    namespace test {
        class Tests {

            internal sealed class Foo {
                public readonly string ClientsName = ""YOLO"";
            }

            public static Foo bad { get; set; }

        }
    }";
			AssertSingleDiagnostic( test, 11, 13, "bad", "it" );
		}


		[Test]
		public void DocumentWithStaticProperty_NonReadonlyUnaudited_NoDiag() {
			const string test = @"
    using System;

    namespace test {
        class Tests {

            internal class Foo {
                public readonly string ClientsName = ""YOLO"";
            }

            [CodeStyle.Statics.Unaudited]
            public static Foo bad { get; set; }

        }
    }";
			AssertNoDiagnostic( test );
		}

		[Test]
		public void DocumentWithStaticProperty_NonReadonlyAudited_NoDiag() {
			const string test = @"
    using System;

    namespace test {
        class Tests {

            internal class Foo {
                public readonly string ClientsName = ""YOLO"";
            }

            [CodeStyle.Statics.Audited]
            public static Foo bad { get; set; }

        }
    }";
			AssertNoDiagnostic( test );
		}

		[Test]
		public void DocumentWithStaticProperty_ReadonlyButMutable_Diag() {
			const string test = @"
    using System;

    namespace test {
        class Tests {

            internal class Foo {
                public string ClientsName = ""YOLO"";
            }

            public static Foo bad { get; }

        }
    }";
			AssertSingleDiagnostic( test, 11, 13, "bad", "test.Tests.Foo" );
		}

		[Test]
		public void DocumentWithStaticProperty_ReadonlyValueType_NoDiag() {
			const string test = @"
    using System;

    namespace test {
        class Tests {

            public static DateTime good { get; }

        }
    }";
			AssertNoDiagnostic( test );
		}

		[Test]
		public void DocumentWithStaticProperty_ReadonlyImmutable_NoDiag() {
			const string test = @"
    using System;

    namespace test {
        class Tests {

            internal sealed class Foo {
                public readonly string ClientsName = ""YOLO"";
            }

            public static Foo bad { get; }

        }
    }";
			AssertNoDiagnostic( test );
		}


		[Test]
		public void DocumentWithStaticProperty_PrivateSetterImmutable_Diag() {
			const string test = @"
    using System;

    namespace test {
        class Tests {

            internal sealed class Foo {
                public readonly string ClientsName = ""YOLO"";
            }

            public static Foo bad { get; private set; }

        }
    }";
			AssertSingleDiagnostic( test, 11, 13, "bad", "it" );
		}

		[Test]
        public void DocumentWithStaticProperty_ImplementedGetter_NoDiag() {
            const string test = @"
    using System;

    namespace test {
        class Tests {

            internal class Foo { 
                private string m_mutable = null;
            }

            // safe, because it's not a static variable at all
            public static Foo good { 
                get { 
                    return new Foo(); 
                } 
            }

        }
    }";
            AssertNoDiagnostic( test );
        }

        [Test]
		public void DocumentWithStaticProperty_ImmutablePropertyWithImmutableMarkedType_NoDiag() {
			const string test = @"
    using System;

    namespace test {
        class Tests {

            [Immutable] // yes, this isn't actually immutable, that's the point
            internal class Foo {
                public string ClientsName = ""YOLO"";
            }

            public static Foo good { get; }

        }
    }";
			AssertNoDiagnostic( test );
		}

		[Test]
        public void DocumentWithStaticProperty_InterfaceWithImmutableConcreteInitializer_NoDiag() {
            const string test = @"
    using System;

    namespace test {
        class Tests {

            interface IFoo {}
            internal sealed class Foo : IFoo {
                public readonly string ClientsName = ""YOLO"";
            }

            public static IFoo good { get; } = new Foo();

        }
    }";
            AssertNoDiagnostic( test );
        }

        [Test]
        public void DocumentWithStaticProperty_InterfaceWithMutableConcreteInitializer_Diag() {
            const string test = @"
    using System;

    namespace test {
        class Tests {

            interface IFoo {}
            internal sealed class Foo : IFoo {
                public string ClientsName = ""YOLO"";
            }

            public static IFoo bad { get; } = new Foo();

        }
    }";
            AssertSingleDiagnostic( test, 12, 13, "bad", "test.Tests.Foo" );
        }

		[Test]
		public void DocumentWithOneLevelRecurrsiveTypes_Immutable_NoDiag() {
			const string test = @"
	using System;

	namespace test {
		class Tests {

            private readonly static Foo foo = new Foo();

			internal sealed class Foo {
				public readonly Foo Instance;
			}
		}
	}";

			AssertNoDiagnostic( test );
		}

		[Test]
		public void DocumentWithOneLevelRecurrsiveTypes_Mutable_Diag() {
			const string test = @"
	using System;

	namespace test {
		class Tests {

            private readonly static Foo foo = new Foo();

			internal class Foo {
				public Foo Instance;
			}
		}
	}";

			AssertSingleDiagnostic( test, 7, 41, "foo", "test.Tests.Foo" );
		}

		[Test]
		public void DocumentWithMultiLevelRecurrsiveTypes_Immutable_NoDiag() {
			const string test = @"
	using System;

	namespace test {
		class Tests {

            private readonly static Foo foo = new Foo();

			internal sealed class Foo {
				public readonly Bar Bar = null;
			}

			internal sealed class Bar {
				public readonly Foo Foo = new Foo();
			}
		}
	}";

			AssertNoDiagnostic( test );
		}


		[Test]
		public void DocumentWithMultiLevelRecurrsiveTypes_Mutable_Diag() {
			const string test = @"
	using System;

	namespace test {
		class Tests {

            private readonly static Foo foo = new Foo();

			internal sealed class Foo {
				public readonly Bar Bar = null; // Bar is not sealed, so this is not immutable
			}

			internal class Bar {
				public readonly Foo Foo = new Foo();
			}
		}
	}";

			AssertSingleDiagnostic( test, 7, 41, "foo", "test.Tests.Foo" );
		}

		[Test]
		public void DocumentWithStaticField_ReadonlyUnsafeBaseClassWithNonConstructorInitializerOfUnsealedType_Diag() {
			const string test = @"
	using System;
	namespace test {
		class Tests {
			interface IUnsafe { void Magic(); } // could be anythinggggggg

			class Safe : IUnsafe {
				void IUnsafe.Magic() {} // looks safe to me
				public static readonly Safe Instance { get; } = new Safe();
			}

			private readonly static IUnsafe foo = Safe.Instance;
		}
	}";

			// Safe isn't sealed
			AssertSingleDiagnostic( test, 12, 36, "foo", "test.Tests.Safe" );
		}

		[Test]
		public void DocumentWithStaticField_ReadonlyUnsafeBaseClassWithNonConstructorInitializerOfSealedType_NoDiag() {
			const string test = @"
	using System;
	namespace test {
		class Tests {
			interface IUnsafe { void Magic(); } // could be anythinggggggg

			sealed class Safe : IUnsafe {
				void IUnsafe.Magic() {} // looks safe to me
				public static readonly Safe Instance { get; } = new Safe();
			}

			private readonly static IUnsafe foo = Safe.Instance;
		}
	}";

			AssertNoDiagnostic( test );
		}

		[Test]
		public void DocumentWithStaticField_ReadonlyUnsafeBaseClassWithSafeInitializer_NoDiag2() {
			const string test = @"
	using System;
	using System.Collections.Generic;
	namespace test {
		class Tests {
			private readonly static IEqualityComparer<string> foo = StringComparer.Ordinal;
		}
	}";

			AssertNoDiagnostic( test );
		}

		[Ignore( "This is an unlikely-to-be-used hole in the analyzer that we need to fix regardless" )]
		public void DocumentWithStaticField_TypeIsUnsafeInitializerIsImplicitConversionFromSafeValue_Diag() {
			const string test = @"
	using System;
	namespace test {
		class Tests {
			sealed class Foo {
				public Foo(int xx) { x = xx; }

				public static implicit operator Foo(int x) {
					return new Foo(x);
				}

				public int x; // makes Foo mutable
			}

			private static readonly Foo foo = 3;
		}
	}";
			AssertSingleDiagnostic( test, 15, 32, "foo", "test.Tests.Foo" );
		}

		private void AssertNoDiagnostic( string file ) {
			VerifyCSharpDiagnostic( file );
		}

		private void AssertSingleDiagnostic( string file, int line, int column, string fieldOrProp, string badFieldOrType ) {

			DiagnosticResult result = CreateDiagnosticResult( line, column, fieldOrProp, badFieldOrType );
			VerifyCSharpDiagnostic( file, result );
		}

		private static DiagnosticResult CreateDiagnosticResult( int line, int column, string fieldOrProp, string badFieldOrType ) {
			return new DiagnosticResult {
				Id = UnsafeStaticsAnalyzer.DiagnosticId,
				Message = string.Format( UnsafeStaticsAnalyzer.MessageFormat, fieldOrProp, badFieldOrType ),
				Severity = DiagnosticSeverity.Error,
				Locations = new[] {
					new DiagnosticResultLocation( "Test0.cs", line, column )
				}
			};
		}
	}
}
