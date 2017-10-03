using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using agsXMPP;
using agsXMPP.protocol;
using agsXMPP.protocol.client;
using agsXMPP.protocol.iq.register;
using agsXMPP.Xml.Dom;

namespace FarmBot.IM {

  class IMC {

    private String host;
    private String login;
    private String pass;
    private String resource;
    private XmppClientConnection xmpp;
    private Jid jid;
    private Boolean xmpp_status;
    private Thread thread_loop;
    private Thread thread_xmpp;
    private Boolean loop_close = false;
    private int xmpp_n;
    private Inbox[] inbox;
    private int inbox_offset_write;
    private int inbox_offset_read;
    private String[] inbox_mess;
    private Boolean outbox_write_status;
    private String[] outbox_mess;
    private Boolean inbox_status_read;
    private Boolean presence_t;
    private String presence_status;
    private int presence_type;
    private const String resource_default = "main";
    private const int TLS_port = 5222;
    private const String app_name = "FarmBot";
    private const String log_tag = "XMPP";
    private const int inbox_size = 32;

    public IMC(String host, String login, String pass){
      this.host = host;
      this.login = login;
      this.pass = pass;
      this.resource = resource_default;
      this.xmpp = null;
      this.xmpp_status = false;
      this.jid = new Jid(this.login + "@" + this.host);
      this.loop_close = false;
      this.xmpp_n = 0;
      this.inbox = new Inbox[inbox_size];
      this.inbox_offset_write = 0;
      this.inbox_offset_read = 0;
      this.inbox_status_read = false;
      this.inbox_mess = new String[2];
      this.inbox_mess[0] = "";
      this.inbox_mess[1] = "";
      this.outbox_write_status = true;
      this.outbox_mess = new String[2];
      this.outbox_mess[0] = "";
      this.outbox_mess[1] = "";
      this.presence_t = false;
      this.presence_status = "";
      this.presence_type = 0;
      this.init();
    }

    private void XMPP(){
      this.xmpp = new XmppClientConnection();
      this.xmpp.Server = this.jid.Server;
      this.xmpp.Port = TLS_port;
      this.xmpp.Username = this.jid.User;
      this.xmpp.Password = this.pass;
      this.xmpp.Resource = this.resource;
      this.xmpp.AutoAgents = false;
      this.xmpp.AutoPresence = false;
      this.xmpp.AutoRoster = true;
      this.xmpp.AutoResolveConnectServer = true;
      this.xmpp.UseStartTLS = true;
      this.xmpp.UseSSL = false;
      this.xmpp.KeepAlive = true;
      this.xmpp.KeepAliveInterval = 120;
      this.xmpp.EnableCapabilities = true;
      this.xmpp.ClientLanguage = "ru";
      this.xmpp.ClientVersion = app_name;
      try {
        this.xmpp.OnLogin += new ObjectHandler(Onlogin);
        this.xmpp.OnMessage += new MessageHandler(OnMessage);
        this.xmpp.OnRegisterInformation += new RegisterEventHandler(OnRegisterInformation);
        this.xmpp.OnIq += new IqHandler(OnIq);
        this.xmpp.OnClose += new ObjectHandler(OnClose);
        this.xmpp.OnAuthError += new XmppElementHandler(OnAuthError);
        this.xmpp.OnError += new ErrorHandler(OnError);
        this.xmpp.OnSocketError += new ErrorHandler(OnSocketError);
        this.xmpp.OnStreamError += new XmppElementHandler(OnStreamError);
        this.xmpp.OnWriteXml +=new XmlHandler(xmpp_OnWriteXml);
        this.xmpp.OnReadXml +=new XmlHandler(xmpp_OnReadXml);
        this.xmpp.Open();
        } catch (Exception e){
        Console.WriteLine(e.Message);
      }
    }

    private void Onlogin(object sender){
      this.xmpp_status = true;
      this.loger("OnLogin");
    }

    private void OnMessage(object sender, Message msg){
      //this.loger("OnMessage");
      if (msg.Body != null){
        if (msg.Body.Length > 0){
          this.InboxController(true);
          this.inbox[this.inbox_offset_write].jid = msg.From;
          this.inbox[this.inbox_offset_write].mess = msg.Body;
        }
      }
    }

    private void OnRegisterInformation(Object sender, RegisterEventArgs args){
      this.loger("OnRegisterInformation");
    }

    private void OnIq(Object sender, IQ iq){
      this.loger("OnIq");
      if (iq.Type == IqType.get){
        iq.SwitchDirection();
        iq.Type = IqType.result;
        this.xmpp.Send(iq);
      }
    }

    private void OnClose(object sender){
      this.xmpp_status = false;
      this.loger("OnClose");
    }

    private void OnAuthError(object sender, Element e){
      this.xmpp_status = false;
      this.loger("OnAuthError");
    }

    private void OnError(object sender, Exception ex){
      this.loger("OnError: " + ex.Message);
    }

    private void OnSocketError(object sender, Exception ex){
      this.xmpp_status = false;
      this.loger("OnSocketError");
    }

    private void OnStreamError(object sender, Element e){
      this.loger("OnStreamError");
    }

    private void xmpp_OnWriteXml(Object sender, string xml){
      //Console.WriteLine("\nwrite: " + xml);
    }

    private void xmpp_OnReadXml(Object sender, string xml){
      //Console.WriteLine("\nread: " + xml);
    }

    private void loger(String show){
      Console.WriteLine(log_tag + ": " + show);
    }

    private Boolean messValidetor(String mess){
      if (mess.Length > 0) return true;
      return false;
    }

    private void loop(){
      while(true){
        //this.loger("loop run");
        if (this.xmpp_n > 1200){
          this.xmpp_n = 0;
          if (!this.xmpp_status){
            this.thread_xmpp.Start();
            this.loger("client start again");
          }
        }
        this.xmpp_n++;
        this.PresenceController();
        this.OutboxController();
        if (this.loop_close){
          this.xmpp.Close();
          break;
        }
        Thread.Sleep(250);
      }
    }

    private void PresenceController(){
      if (this.presence_t && this.xmpp_status){
        Presence pr = new Presence();
        pr.Status = this.presence_status;
        pr.Type = PresenceType.available;
        switch(this.presence_type){
          case 0:
            pr.Show = ShowType.chat;
            break;
          case 1:
            pr.Show = ShowType.away;
            break;
          case 2:
            pr.Show = ShowType.dnd;
            break;
          case 3:
            pr.Show = ShowType.xa;
            break;
          case 4:
            pr.Show = ShowType.NONE;
            break;
          default:
            pr.Show = ShowType.chat;
            break;
        }
        this.xmpp.Send(pr);
        this.presence_t = false;
        this.loger("Presence update");
      }
    }

    private void InboxController(Boolean mode){
      if (mode){
        this.inbox_offset_write++;
        if (this.inbox_offset_write >= inbox_size) this.inbox_offset_write = 0;
        } else {
        if (this.inbox_offset_read != this.inbox_offset_write){
          this.inbox_mess[0] = "";
          this.inbox_mess[1] = "";
          this.inbox_offset_read++;
          if (this.inbox_offset_read >= inbox_size) this.inbox_offset_read = 0;
          this.inbox_status_read = true;
          this.inbox_mess[0] = this.inbox[this.inbox_offset_read].jid.ToString();
          this.inbox_mess[1] = this.inbox[this.inbox_offset_read].mess;
          } else {
          this.inbox_status_read = false;
        }
      }
    }

    private void OutboxController(){
      if (this.xmpp_status && !this.outbox_write_status){
        this.xmpp.Send(new Message(new Jid(this.outbox_mess[0]), MessageType.chat, this.outbox_mess[1]));
        this.outbox_write_status = true;
      }
    }

    private void init(){
      this.thread_xmpp = new System.Threading.Thread(new System.Threading.ThreadStart(this.XMPP));
      this.thread_xmpp.Start();
      this.thread_loop = new System.Threading.Thread(new System.Threading.ThreadStart(this.loop));
      this.thread_loop.Start();
    }

    public void stop(){
      this.loop_close = true;
    }

    public void setPresenceType(int type){
      this.presence_type = type;
      this.presence_t = true;
    }

    public void setPresenceStatus(String status){
      this.presence_status = status;
      this.presence_t = true;
    }

    public Boolean doInbox(){
      this.InboxController(false);
      return this.inbox_status_read;
    }

    public String[] getInboxMess(){
      return this.inbox_mess;
    }

    public void setMess(String[] mess){
      this.outbox_mess = mess;
      this.outbox_write_status = false;
    }

    public Boolean getOutboxStatus(){
      return this.outbox_write_status;
    }

  }

  public struct Inbox {
    public Jid jid;
    public String mess;
  }

}