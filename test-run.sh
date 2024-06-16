#!/bin/bash

echo "Starting setup of a local test environment for FirecrackerSharp's test suite"

DOTNET_SDK_VERSION="8.0"
DOTNET="dotnet"
GIT_REPOSITORY="https://github.com/kanpov/FirecrackerSharp.git"
TEST_DATA_URL="https://kanpov.github.io/cdn/firecrackersharp/testdata-1.0.0.tar.gz"
DEPENDENCIES="podman git curl tar"

if [ "$EUID" -ne 0 ]
then
  ROOTLESS="yes"
  echo "Rootless mode of execution enabled, beware of potential issues!"
else
  ROOTLESS="no"
fi

function install_deps() {
  if command -v apt &> /dev/null
  then
    echo "Installing dependencies via apt..."
    sudo apt install -y $DEPENDENCIES
  elif command -v dnf &> /dev/null
  then
    echo "Installing dependencies via dnf..."
    sudo dnf install -y $DEPENDENCIES
  elif command -v zypper &> /dev/null
  then
    echo "Installing dependencies via zypper..."
    sudo zypper --non-interactive in $DEPENDENCIES
  else
    echo "Your package manager is unsupported, ensure these are installed: $DEPENDENCIES"
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
    mkdir ~/.firecracker/dotnet
    
    wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
    chmod +x dotnet-install.sh
    ./dotnet-install.sh --channel $DOTNET_SDK_VERSION --install-dir ~/.firecracker/dotnet
    
    DOTNET="~/.firecracker/dotnet/dotnet"
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

function clone_repo() {
  mkdir ~/.firecracker/repo
  cd ~/.firecracker/repo
  if [ ! -e FirecrackerSharp ]
  then
    git clone $GIT_REPOSITORY
  fi
  cd FirecrackerSharp
  git fetch
  git pull
}

function build_ssh_server_image() {
  cd FirecrackerSharp.Host.Ssh.Tests
  podman build -t ssh_server:latest .
  cd ../../..
}

function download_test_data() {
  sudo mkdir /opt/testdata
  sudo chown -R $USER /opt/testdata
  if [ ! -e /opt/testdata/firecracker ]
  then
    if [ ! -e test-data.tar.gz ]
    then
      echo "Downloading test data archive"
      curl $TEST_DATA_URL -o test-data.tar.gz
    fi
    
    echo "Extracting test data archive"
    tar -xvzf test-data.tar.gz -C /opt
    rm test-data.tar.gz
  fi
}

function run_tests() {
  cd repo/FirecrackerSharp
  bash -c "$DOTNET test"
}

mkdir ~/.firecracker

install_deps
install_dotnet
check_kvm
clone_repo
build_ssh_server_image
download_test_data
run_tests
