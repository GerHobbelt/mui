﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace FirstFloor.ModernUI.Presentation
{
    /// <summary>
    ///     The base implementation of the INotifyPropertyChanged contract.
    /// </summary>
    public abstract class NotifyPropertyChanged
        : DependencyObject, INotifyPropertyChanged
    {
        /// <summary>
        ///     Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        ///     Raises the PropertyChanged event.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        /// <summary>
        /// Occurs when the value of the property changes (avoid hard coding and avoid mistakes in attribute names)
		/// 
		/// `OnPropertyChanged(()=>this.Stocks);`
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="propertyExpression">Attribute expression</param>
        protected virtual void OnPropertyChanged<T>(Expression<Func<T>> propertyExpression)
        {
            var propertyName = (propertyExpression.Body as MemberExpression).Member.Name;
            this.OnPropertyChanged(propertyName);
        }

#if !NET4
        /// <summary>
        ///     Updates specified value, and raises the <see cref="PropertyChanged" /> event when the value has changed.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="storage">The current stored value</param>
        /// <param name="value">The new value</param>
        /// <param name="propertyName">The optional property name, automatically set to caller member name when not set.</param>
        /// <returns>Indicates whether the value has changed.</returns>
        protected bool Set<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (!Equals(storage, value))
            {
                storage = value;
                OnPropertyChanged(propertyName);
                return true;
            }
            return false;
        }
#endif
    }
}
