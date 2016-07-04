﻿/****************************************************************************
Copyright (c) 2013-2015 scutgame.com

http://www.scutgame.com

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
****************************************************************************/
using AccountServer.Handler.Data;
using AccountServer.Lang;
using ZyGames.Framework.Game.Sns;
using ZyGames.Framework.Game.Sns.Service;

namespace AccountServer.Handler
{
    /// <summary>
    /// Change password
    /// </summary>
    public class Password : BaseHandler, IHandler<PassportInfo>
    {
        public ResponseData Excute(PassportInfo data)
        {
            if (string.IsNullOrEmpty(data.PassportId) || string.IsNullOrEmpty(data.Password))
            {
                throw new HandlerException(StateCode.Error, StateDescription.PasswordOrPassError);
            }
            data.Password = DecodePassword(data.Password);
            int result = SnsManager.ChangePass(data.PassportId, data.Password);
            if (result <= 0)
            {
                throw new HandlerException(StateCode.Error, StateDescription.ChangePassError);
            }
            return new ResponseData();
        }

    }
}