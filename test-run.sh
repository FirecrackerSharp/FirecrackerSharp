#!/bin/bash

echo "Starting setup of a local test environment for FirecrackerSharp's test suite. Only problems will be logged"

DOTNET_SDK_VERSION="8.0"
DOTNET="dotnet"
DOTNET_OPTIONAL_INSTALL_LOCATION="/opt/firecracker-dotnet"

if [ "$EUID" -ne 0 ]
then
  ROOTLESS="yes"
  echo "Rootless mode of execution enabled, beware of potential issues!"
else
  ROOTLESS="no"
fi

bold=$(tput bold)
normal=$(tput sgr0)

function install_podman() {
  if ! command -v podman &> /dev/null
  then
    echo "Podman is not installed"
    
    if command -v apt &> /dev/null
    then
      echo "Installing Podman via apt..."
      sudo apt install -y podman
    elif command -v dnf &> /dev/null
    then
      echo "Installing Podman via dnf..."
      sudo dnf install -y podman
    elif command -v zypper &> /dev/null
    then
      echo "Installing Podman via zypper..."
      sudo zypper --non-interactive in podman
    else
      echo "Your package manager is unsupported, install Podman manually"
      exit 1
    fi
  fi
  
  if [[ $ROOTLESS == "yes" ]]
  then
    systemctl start --user podman.socket
  else
    systemctl start podman.socket
  fi
  
  if [ ! -f ~/.testcontainers.properties ]
  then
    echo "No Testcontainers configuration, creating one..."
    echo "docker.host=/run/podman/podman.sock" > ~/.testcontainers.properties
    echo "Testcontainers configured to point to Podman socket"
  fi
}

function install_dotnet() {
  if ! command -v dotnet &> /dev/null
  then
    echo "The .NET SDK could not be found, installing it..."
    mkdir $DOTNET_OPTIONAL_INSTALL_LOCATION
    
    wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
    chmod +x dotnet-install.sh
    ./dotnet-install.sh --channel $DOTNET_SDK_VERSION --install-dir $DOTNET_OPTIONAL_INSTALL_LOCATION
    
    DOTNET="/opt/firecracker-dotnet/dotnet"
  fi
}

function check_kvm() {
  CHECK_OUTPUT=`lsmod | grep kvm`
  if ! [[ $CHECK_OUTPUT =~ "kvm" ]]
  then
    echo "KVM is not present on the system. If this is bare-metal, virtualization is not enabled or KVM needs to be installed. If this is a VM, you need to enable KVM pass-through"
    exit 1
  fi
  
  if ! [[ $(stat -c "%A" /dev/kvm) =~ "rw" ]]
  then
    echo "Access to KVM hasn't been granted to this non-root user. Trying to force access through"
    [ $(stat -c "%G" /dev/kvm) = kvm ] && sudo usermod -aG kvm ${USER}
    sudo setfacl -m u:${USER}:rw /dev/kvm
    
    if ! [[ $(stat -c "%A" /dev/kvm) =~ "rw" ]]
    then
      echo "Attempts at forcing access to KVM failed, you will need to do this manually"
      exit 1
    else
      echo "Access to KVM was granted successfully"
    fi
  fi
}

install_podman
install_dotnet
check_kvm

#$DOTNET --info
