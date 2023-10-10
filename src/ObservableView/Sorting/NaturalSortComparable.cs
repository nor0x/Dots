using System;
using System.Collections.Generic;

namespace ObservableView.Sorting
{
    /// <summary>
    /// Class NaturalSortComparable.
    /// Can be used to sort strings with 'natural sort order'.
    /// 
    /// The comparison algorithm is based on logic which was published here:
    /// https://psycodedeveloper.wordpress.com/2013/04/11/numeric-sort-file-system-names-in-c-like-windows-explorer/
    /// </summary>
    public class NaturalSortComparable : IComparable,
                                         IComparable<NaturalSortComparable>
    {
        private readonly string value;

        public NaturalSortComparable(string value)
        {
            this.value = value;
        }

        public int CompareTo(object otherValueObj)
        {
            return this.CompareTo(otherValueObj as NaturalSortComparable);
        }

        public int CompareTo(NaturalSortComparable other)
        {
            if (this.value == null && other == null)
            {
                return 0;
            }

            if (this.value == null)
            {
                return -1;
            }

            if (other == null)
            {
                return 1;
            }

            var otherValue = other;

            int xIndex = 0;
            int yIndex = 0;

            while (xIndex < this.value.Length)
            {
                if (yIndex >= otherValue.Length)
                {
                    return 1;
                }

                if (char.IsDigit(this.value[xIndex]))
                {
                    if (!char.IsDigit(otherValue[yIndex]))
                    {
                        return -1;
                    }

                    // Compare the numbers 
                    var xText = new List<char>();
                    var yText = new List<char>();

                    for (int i = xIndex; i < this.value.Length; i++)
                    {
                        var xChar = this.value[i];

                        if (char.IsDigit(xChar))
                        {
                            xText.Add(xChar);
                        }
                        else
                        {
                            break;
                        }
                    }

                    for (int j = yIndex; j < otherValue.Length; j++)
                    {
                        var yChar = otherValue[j];
                        if (char.IsDigit(yChar))
                        {
                            yText.Add(yChar);
                        }
                        else
                        {
                            break;
                        }
                    }

                    var xValue = Convert.ToDecimal(new string(xText.ToArray()));
                    var yValue = Convert.ToDecimal(new string(yText.ToArray()));

                    if (xValue < yValue)
                    {
                        return -1;
                    }
                    else if (xValue > yValue)
                    {
                        return 1;
                    }

                    // Skip 
                    xIndex += xText.Count;
                    yIndex += yText.Count;
                }
                else if (char.IsDigit(otherValue[yIndex]))
                    return 1;
                else
                {
                    int difference = char.ToUpperInvariant(this.value[xIndex]).CompareTo(char.ToUpperInvariant(otherValue[yIndex]));
                    if (difference > 0)
                    {
                        return 1;
                    }
                    else if (difference < 0)
                    {
                        return -1;
                    }

                    xIndex++;
                    yIndex++;
                }
            }

            if (yIndex < otherValue.Length)
            {
                return -1;
            }

            return 0;
        }

        private char this[int index]
        {
            get
            {
                return this.value[index];
            }
        }

        private int Length
        {
            get
            {
                return this.value == null ? 0 : this.value.Length;
            }
        }

        public override string ToString()
        {
            return this.value ?? string.Empty;
        }
    }
}
