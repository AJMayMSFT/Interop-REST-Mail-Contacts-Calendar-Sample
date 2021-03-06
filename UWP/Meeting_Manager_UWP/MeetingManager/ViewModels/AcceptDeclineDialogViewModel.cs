﻿//Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license.
//See LICENSE in the project root for license information.

using MeetingManager.Models;
using Prism.Commands;
using System;

namespace MeetingManager.ViewModels
{
    class AcceptDeclineDialogViewModel : DialogViewModel
    {
        private string _action;
        private string _meetingId;

        public DelegateCommand SendCommand => new DelegateCommand(Send);

        public string Title { get; set; }

        public string Comment { get; set; }

        protected override void OnInitialize(InitDialog parameter)
        {
            base.OnInitialize(parameter);

            var payload = UI.Deserialize<Tuple<string, string>>(parameter.Payload);

            _action = payload.Item1.ToLower();
            _meetingId = payload.Item2;

            switch (_action)
            {
                case OData.Accept:
                    Title = GetString("AcceptTitle");
                    break;
                case OData.TentativelyAccept:
                    Title = GetString("TentativeTitle");
                    break;
                case OData.Decline:
                    Title = GetString("DeclineTitle");
                    break;
            }

            OnPropertyChanged(() => Title);
        }

        private async void Send()
        {
            await OfficeService.AcceptOrDecline(_meetingId, _action, Comment);
        }
    }
}
