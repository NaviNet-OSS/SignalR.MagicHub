using System.Globalization;
using Antlr4.Runtime;

namespace SignalR.MagicHub.Filtering.Parsing
{
    public class CaseInsensitiveInputStream : AntlrInputStream
    {

        protected char[] _lookahead;

        public CaseInsensitiveInputStream(string input)
            : base(input)
        {
            _lookahead = input.ToUpper(CultureInfo.CurrentCulture).ToCharArray();
        }
        /// <summary>
        /// Gets the value of the symbol at offset
        /// <code>
        /// i
        /// </code>
        ///             from the current
        ///             position. When
        /// <code>
        /// i==1
        /// </code>
        ///             , this method returns the value of the current
        ///             symbol in the stream (which is the next symbol to be consumed). When
        /// <code>
        /// i==-1
        /// </code>
        ///             , this method returns the value of the previously read
        ///             symbol in the stream. It is not valid to call this method with
        /// <code>
        /// i==0
        /// </code>
        ///             , but the specific behavior is unspecified because this
        ///             method is frequently called from performance-critical code.
        ///             <p>This method is guaranteed to succeed if any of the following are true:</p><ul><li>
        /// <code>
        /// i&gt;0
        /// </code>
        /// </li><li>
        /// <code>
        /// i==-1
        /// </code>
        ///             and
        ///             <see cref="P:Antlr4.Runtime.IIntStream.Index">index()</see>
        ///             returns a value greater
        ///             than the value of
        /// <code>
        /// index()
        /// </code>
        ///             after the stream was constructed
        ///             and
        /// <code>
        /// LA(1)
        /// </code>
        ///             was called in that order. Specifying the current
        /// <code>
        /// index()
        /// </code>
        ///             relative to the index after the stream was created
        ///             allows for filtering implementations that do not return every symbol
        ///             from the underlying source. Specifying the call to
        /// <code>
        /// LA(1)
        /// </code>
        ///             allows for lazily initialized streams.</li><li>
        /// <code>
        /// LA(i)
        /// </code>
        ///             refers to a symbol consumed within a marked region
        ///             that has not yet been released.</li></ul><p>If
        /// <code>
        /// i
        /// </code>
        ///             represents a position at or beyond the end of the stream,
        ///             this method returns
        ///             <see cref="F:Antlr4.Runtime.IntStreamConstants.Eof"/>
        ///             .</p><p>The return value is unspecified if
        /// <code>
        /// i&lt;0
        /// </code>
        ///             and fewer than
        /// <code>
        /// -i
        /// </code>
        ///             calls to
        ///             <see cref="M:Antlr4.Runtime.IIntStream.Consume">consume()</see>
        ///             have occurred from the beginning of
        ///             the stream before calling this method.</p>
        /// </summary>
        /// <exception cref="T:System.NotSupportedException">if the stream does not support
        ///             retrieving the value of the specified symbol
        ///             </exception>
        public override int La(int i)
        {
            if (i == 0)
            {
                return 0; // undefined
            }
            if (i < 0)
            {
                i++; // e.g., translate LA(-1) to use offset i=0; then data[p+0-1]
                if ((p + i - 1) < 0)
                {

                    return IntStreamConstants.Eof; // invalid; no char before first char
                }
            }

            if ((p + i - 1) >= n)
            {
                return IntStreamConstants.Eof;
            }

            return _lookahead[p + i - 1];
        }

    }
}
