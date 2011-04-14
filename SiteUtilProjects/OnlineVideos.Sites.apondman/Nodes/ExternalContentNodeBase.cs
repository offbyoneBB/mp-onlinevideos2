using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Net;
using System.Xml;

namespace OnlineVideos.Sites.Pondman {
    
    using Interfaces;
    using OnlineVideos.Sites.Pondman.Nodes;

    public abstract class ExternalContentNodeBase : IExternalContentNode  {

        ISession IExternalContentNode.Session
        {
            get
            {
                return this.session;
            }
            set
            {
                this.session = value;
            }
        } protected ISession session;
        
        public virtual string Uri
        {
            get
            {
                return this.uri;
            }
            set
            {
                this.uri = value;
            }
        } protected string uri;

        public virtual NodeState State
        {
            get
            {
                return this.state;
            }
        } protected NodeState state = NodeState.Initial;

        public virtual NodeResult Update()
        {
            return NodeResult.Success;
        }

        public override bool Equals(object obj) {
            return Equals(obj as ExternalContentNodeBase);
        }

        public virtual bool Equals(ExternalContentNodeBase node) {
            if (node == null) 
            {
                return false;
            }

            return (this.uri == node.uri);
        }

        /// <summary>
        /// Returns the hash code for this node (based on it's URI)
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode() {
            return uri.GetHashCode();
        }

    }
}
