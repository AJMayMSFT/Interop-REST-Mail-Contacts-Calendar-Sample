﻿//Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license.
//See LICENSE in the project root for license information.

using MeetingManager.Models;
using Prism.Commands;
using Prism.Windows.AppModel;
using Prism.Windows.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Navigation;

namespace MeetingManager.ViewModels
{
    class EmailPageViewModel : ViewModel
    {
        private EventMessage _message;
        private string _action;
        private string _comment;
        private bool _mailHasBeenSent;
        private ObservableCollection<Message.Recipient> _recipients;

        public EmailPageViewModel()
        {
            UI.Subscribe<User>(UserSelected);
            UI.Subscribe<Contact>(ContactSelected);
        }

        public DelegateCommand SendCommand => new DelegateCommand(SendMail, CanExecuteSendMail);
        public DelegateCommand AddUserRecipientCommand => new DelegateCommand(AddUserRecipient);
        public DelegateCommand AddContactRecipientCommand => new DelegateCommand(AddContactRecipient);
        public DelegateCommand<Message.Recipient> DeleteRecipientCommand => new DelegateCommand<Message.Recipient>(DeleteRecipient);

        [RestorableState]
        public EventMessage Message
        {
            get { return _message; }
            private set { SetProperty(ref _message, value); }
        }

        public string Title => GetString("SendMailTitle");

        public string Comment
        {
            get { return _comment; }
            set { SetProperty(ref _comment, value); }
        }

        public ObservableCollection<Message.Recipient> Recipients
        {
            get { return _recipients; }
            private set { SetProperty(ref _recipients, value); }
        }

        public bool IsContentText { get; set; }

        public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
        {
            base.OnNavigatedTo(e, viewModelState);

            if (e.NavigationMode == NavigationMode.New)
            {
                var tuple = UI.Deserialize<Tuple<EventMessage, string, string>>(e.Parameter);

                Message = tuple.Item1;
                _action = tuple.Item2.ToLower();
                Comment = tuple.Item3;

                _mailHasBeenSent = false;

                IsContentText = Message.Body.ContentType.EqualsCaseInsensitive("text");

                PopulateRecipients();
             }
        }

        public override async void OnNavigatingFrom(NavigatingFromEventArgs e, Dictionary<string, object> viewModelState, bool suspending)
        {
            if (e.NavigationMode == NavigationMode.Back && !_mailHasBeenSent)
            {
                await OfficeService.DeleteDraftMessage(Message.Id);
            }
        }

        private void PopulateRecipients()
        {
            Recipients = new ObservableCollection<Message.Recipient>(Message.ToRecipients);
        }

        private async void SendMail()
        {
            using (new Loading(this))
            {
                _mailHasBeenSent = await OfficeService.UpdateAndSendMessage(Message, Comment, Recipients);
            }
            UI.GoBack();
        }

        private async void AddUserRecipient()
        {
            await NavigateToUsers(true);
        }

        private async void AddContactRecipient()
        {
            await NavigateToContacts();
        }

        private void DeleteRecipient(Message.Recipient recipient)
        {
            int pos = Recipients.IndexOf(x => x.EmailAddress.IsEqualTo(recipient.EmailAddress));
            Recipients.RemoveAt(pos);
            SendCommand.RaiseCanExecuteChanged();
        }


        private void UserSelected(User user)
        {
            var recipient = new Message.Recipient
            {
                EmailAddress = new EmailAddress
                {
                    Address = user.UserPrincipalName,
                    Name = user.DisplayName
                }
            };

            AddRecipient(recipient);
        }

        private void ContactSelected(Contact contact)
        {
            var recipient = new Message.Recipient
            {
                EmailAddress = contact.EmailAddresses[0]
            };

            AddRecipient(recipient);
        }

        private void AddRecipient(Message.Recipient recipient)
        {
            if (Recipients.FirstOrDefault(x => x.EmailAddress.IsEqualTo(recipient.EmailAddress)) == null)
            {
                Recipients.Add(recipient);
                SendCommand.RaiseCanExecuteChanged();
            }
        }

        private bool CanExecuteSendMail()
        {
            return Recipients.Any();
        }
    }
}
